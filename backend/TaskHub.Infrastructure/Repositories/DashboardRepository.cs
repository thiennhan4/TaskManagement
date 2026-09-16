using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(DashboardScopeCriteria criteria, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var projectQuery = BuildProjectScope(criteria);
        var taskQuery = BuildTaskScope(projectQuery);

        var total = await taskQuery.CountAsync(ct);
        var todo = await taskQuery.CountAsync(t => t.Status == TaskItemStatus.Todo, ct);
        var inProgress = await taskQuery.CountAsync(t => t.Status == TaskItemStatus.InProgress, ct);
        var done = await taskQuery.CountAsync(t => t.Status == TaskItemStatus.Done, ct);
        var overdue = await taskQuery.CountAsync(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value < now &&
            t.Status != TaskItemStatus.Done,
            ct);

        var totalBoards = await _context.Boards
            .AsNoTracking()
            .Where(b => b.ProjectId.HasValue && projectQuery.Any(p => p.Id == b.ProjectId.Value))
            .CountAsync(ct);

        var teamMembers = criteria.Scope == DashboardScope.Team && criteria.TeamId.HasValue
            ? await _context.TeamMembers
                .AsNoTracking()
                .Where(tm => tm.TeamId == criteria.TeamId.Value)
                .Select(tm => tm.UserId)
                .Distinct()
                .CountAsync(ct)
            : 0;

        return new DashboardStatsDto
        {
            Total = total,
            Todo = todo,
            InProgress = inProgress,
            Done = done,
            Overdue = overdue,
            TotalBoards = totalBoards,
            TeamMembers = teamMembers
        };
    }

    public async Task<List<DashboardVelocityPointDto>> GetVelocityAsync(
        DashboardScopeCriteria criteria, DashboardTimeframe timeframe, DateTime asOfUtc, CancellationToken ct = default)
    {
        var buckets = BuildVelocityBuckets(timeframe, asOfUtc);
        var firstStart = buckets[0].Start;
        var taskQuery = BuildTaskScope(BuildProjectScope(criteria));

        var createdDates = await taskQuery
            .Where(t => t.CreatedAt >= firstStart && t.CreatedAt <= asOfUtc)
            .Select(t => t.CreatedAt)
            .ToListAsync(ct);

        var completions = await _context.TaskActivityLogs
            .AsNoTracking()
            .Where(log => log.Action == ActivityLogAction.StatusChanged &&
                log.NewValue == nameof(TaskItemStatus.Done) &&
                log.OldValue != nameof(TaskItemStatus.Done) &&
                log.CreatedAt >= firstStart && log.CreatedAt <= asOfUtc &&
                taskQuery.Any(t => t.Id == log.TaskId))
            .Select(log => new { log.TaskId, log.CreatedAt })
            .ToListAsync(ct);

        foreach (var bucket in buckets)
        {
            bucket.Created = createdDates.Count(date => date >= bucket.Start && date < bucket.End);
            bucket.Completed = completions
                .Where(log => log.CreatedAt >= bucket.Start && log.CreatedAt < bucket.End)
                .Select(log => log.TaskId)
                .Distinct()
                .Count();
        }

        return buckets;
    }

    private static List<DashboardVelocityPointDto> BuildVelocityBuckets(DashboardTimeframe timeframe, DateTime now)
    {
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return timeframe switch
        {
            DashboardTimeframe.Week => Enumerable.Range(0, 7)
                .Select(day => today.AddDays(-(((int)today.DayOfWeek + 6) % 7) + day))
                .Select(start => new DashboardVelocityPointDto { Start = start, End = start.AddDays(1) })
                .ToList(),
            DashboardTimeframe.Month => Enumerable.Range(0, DateTime.DaysInMonth(now.Year, now.Month))
                .Select(day => monthStart.AddDays(day))
                .Select(start => new DashboardVelocityPointDto { Start = start, End = start.AddDays(1) })
                .ToList(),
            DashboardTimeframe.SixMonths => Enumerable.Range(0, 6)
                .Select(month => monthStart.AddMonths(month - 5))
                .Select(start => new DashboardVelocityPointDto { Start = start, End = start.AddMonths(1) })
                .ToList(),
            DashboardTimeframe.Year => Enumerable.Range(0, 12)
                .Select(month => new DateTime(now.Year, month + 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .Select(start => new DashboardVelocityPointDto { Start = start, End = start.AddMonths(1) })
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(timeframe))
        };
    }

    public async Task<DashboardAnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(
        DashboardScopeCriteria criteria, Guid? boardId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var taskQuery = BuildTaskScope(BuildProjectScope(criteria));
        if (boardId.HasValue)
        {
            taskQuery = taskQuery.Where(task => task.List.BoardId == boardId.Value);
        }

        var tasks = await taskQuery.Select(task => new DashboardAnalyticsTaskDto
        {
            Id = task.Id,
            Status = task.Status,
            Priority = task.Priority,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate
        }).ToListAsync(ct);

        var completions = await _context.TaskActivityLogs.AsNoTracking()
            .Where(log => log.Action == ActivityLogAction.StatusChanged &&
                log.NewValue == nameof(TaskItemStatus.Done) &&
                log.OldValue != nameof(TaskItemStatus.Done) &&
                log.CreatedAt >= from && log.CreatedAt < to &&
                taskQuery.Any(task => task.Id == log.TaskId))
            .Select(log => new DashboardCompletionDto
            {
                TaskId = log.TaskId,
                CompletedAt = log.CreatedAt
            })
            .ToListAsync(ct);

        var timeTrackedSeconds = await _context.TimeEntries.AsNoTracking()
            .Where(entry => entry.EndTime.HasValue &&
                entry.StartTime >= from && entry.StartTime < to &&
                taskQuery.Any(task => task.Id == entry.TaskId))
            .SumAsync(entry => entry.DurationSeconds, ct);

        return new DashboardAnalyticsSnapshotDto
        {
            Tasks = tasks,
            Completions = completions,
            TimeTrackedSeconds = timeTrackedSeconds
        };
    }

    private IQueryable<Project> BuildProjectScope(DashboardScopeCriteria criteria)
    {
        var query = _context.Projects.AsNoTracking();

        if (criteria.Scope == DashboardScope.Personal)
        {
            return query.Where(p =>
                p.ProjectType == ProjectType.Personal &&
                p.WorkspaceId == null &&
                p.OwnerId == criteria.UserId);
        }

        var teamId = criteria.TeamId!.Value;
        return query.Where(p =>
            p.ProjectType == ProjectType.Team &&
            p.WorkspaceId == teamId &&
            (p.OwnerId == criteria.UserId ||
             p.Members.Any(pm => pm.UserId == criteria.UserId) ||
             _context.TeamMembers.Any(tm => tm.TeamId == teamId && tm.UserId == criteria.UserId)));
    }

    private IQueryable<TaskItem> BuildTaskScope(IQueryable<Project> projectQuery)
    {
        return _context.Tasks
            .AsNoTracking()
            .Where(t =>
                !t.IsDeleted &&
                t.List.Board.ProjectId.HasValue &&
                projectQuery.Any(p => p.Id == t.List.Board.ProjectId.Value));
    }
}
