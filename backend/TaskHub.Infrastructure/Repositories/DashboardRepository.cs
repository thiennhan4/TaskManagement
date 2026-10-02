using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public partial class DashboardRepository : IDashboardRepository
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

        var counts = await taskQuery.GroupBy(t => 1).Select(g => new DashboardStatsDto
        {
            Total = g.Count(), Todo = g.Count(t => t.Status == TaskItemStatus.Todo),
            InProgress = g.Count(t => t.Status == TaskItemStatus.InProgress),
            Done = g.Count(t => t.Status == TaskItemStatus.Done),
            Overdue = g.Count(t => t.DueDate < now && t.Status != TaskItemStatus.Done)
        }).SingleOrDefaultAsync(ct) ?? new();

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
            Total = counts.Total,
            Todo = counts.Todo,
            InProgress = counts.InProgress,
            Done = counts.Done,
            Overdue = counts.Overdue,
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

        var daily = timeframe is DashboardTimeframe.Week or DashboardTimeframe.Month;
        var created = await taskQuery.Where(t => t.CreatedAt >= firstStart && t.CreatedAt <= asOfUtc)
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month, Day = daily ? t.CreatedAt.Day : 1 })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Count = g.Count() }).ToListAsync(ct);
        var completed = await _context.TaskActivityLogs.AsNoTracking()
            .Where(log => log.Action == ActivityLogAction.StatusChanged && log.NewValue == nameof(TaskItemStatus.Done) &&
                log.OldValue != nameof(TaskItemStatus.Done) && log.CreatedAt >= firstStart && log.CreatedAt <= asOfUtc &&
                taskQuery.Any(t => t.Id == log.TaskId))
            .GroupBy(log => new { log.CreatedAt.Year, log.CreatedAt.Month, Day = daily ? log.CreatedAt.Day : 1 })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Count = g.Select(log => log.TaskId).Distinct().Count() }).ToListAsync(ct);
        foreach (var bucket in buckets)
        {
            bucket.Created = created.Where(x => x.Year == bucket.Start.Year && x.Month == bucket.Start.Month && x.Day == bucket.Start.Day).Sum(x => x.Count);
            bucket.Completed = completed.Where(x => x.Year == bucket.Start.Year && x.Month == bucket.Start.Month && x.Day == bucket.Start.Day).Sum(x => x.Count);
        }

        return buckets;
    }

    public async Task<PagedResult<DashboardActivityDto>> GetActivityAsync(
        DashboardScopeCriteria criteria, int page, int pageSize, CancellationToken ct = default)
    {
        var projects = BuildProjectScope(criteria);
        var tasks = BuildTaskScope(projects);
        var projectLogs = _context.ProjectActivityLogs.AsNoTracking()
            .Where(log => projects.Any(project => project.Id == log.ProjectId));
        var taskLogs = _context.TaskActivityLogs.AsNoTracking()
            .Where(log => tasks.Any(task => task.Id == log.TaskId));

        // Keep both sources scalar and store-type compatible until after UNION ALL and paging.
        var projectRows = projectLogs.Select(log => new {
            log.Id, ActorId = log.UserId, ActorName = log.User.FullName, Kind = 0,
            EntityId = log.ProjectId, ProjectId = log.ProjectId,
            TeamId = log.Project.WorkspaceId, ProjectName = log.Project.Name, EntityName = log.Project.Name,
            Detail = (string?)null, log.CreatedAt
        });
        var taskRows = taskLogs.Select(log => new {
            log.Id, ActorId = log.UserId, ActorName = log.User.FullName, Kind = 1,
            EntityId = log.TaskId, ProjectId = log.Task.List.Board.ProjectId!.Value,
            TeamId = log.Task.List.Board.Project!.WorkspaceId, ProjectName = log.Task.List.Board.Project!.Name,
            EntityName = log.Task.Title, Detail = log.Action == ActivityLogAction.StatusChanged ? log.NewValue : null, log.CreatedAt
        });
        var union = projectRows.Concat(taskRows);
        var total = await union.CountAsync(ct);
        var rows = await union.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ThenBy(x => x.Kind)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        // The two action enums are stored as strings. Read only this page's action values
        // through their own converters instead of casting stored names to SQL integers.
        var projectIds = rows.Where(x => x.Kind == 0).Select(x => x.Id).ToArray();
        var taskIds = rows.Where(x => x.Kind == 1).Select(x => x.Id).ToArray();
        var projectActions = await projectLogs.Where(x => projectIds.Contains(x.Id)).Select(x => new { x.Id, x.Action }).ToDictionaryAsync(x => x.Id, x => x.Action, ct);
        var taskActions = await taskLogs.Where(x => taskIds.Contains(x.Id)).Select(x => new { x.Id, x.Action }).ToDictionaryAsync(x => x.Id, x => x.Action, ct);
        return new PagedResult<DashboardActivityDto> {
            Page = page, PageSize = pageSize, TotalItems = total,
            Items = rows.Select(x => new DashboardActivityDto {
                Id=x.Id, ActorId=x.ActorId, ActorName=x.ActorName,
                EventType=x.Kind == 0 ? projectActions[x.Id].ToString() : taskActions[x.Id].ToString(),
                EntityType=x.Kind == 0 ? "Project" : "Task", EntityId=x.EntityId, ProjectId=x.ProjectId,
                TeamId=x.TeamId, ProjectName=x.ProjectName, EntityName=x.EntityName, Detail=x.Detail, CreatedAt=x.CreatedAt
            }).ToList()
        };
    }

    public async Task<PagedResult<DashboardTaskDto>> GetUpcomingAsync(
        DashboardScopeCriteria criteria, DateTime fromUtc, DateTime toUtc, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildTaskScope(BuildProjectScope(criteria))
            .Where(task => task.Status != TaskItemStatus.Done &&
                task.DueDate.HasValue &&
                task.DueDate.Value >= fromUtc && task.DueDate.Value < toUtc);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(task => task.DueDate).ThenBy(task => task.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(task => new
            {
                task.Id, task.Title, task.Status, task.Priority, task.DueDate,
                task.Progress, task.ListId, ListName = task.List.Name, task.CreatedAt
            }).ToListAsync(ct);

        return new PagedResult<DashboardTaskDto>
        {
            Items = rows.Select(task => new DashboardTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                DueDate = task.DueDate,
                Progress = task.Progress,
                ListId = task.ListId,
                ListName = task.ListName,
                CreatedAt = task.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
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
            var personal = query.Where(p =>
                p.ProjectType == ProjectType.Personal &&
                p.WorkspaceId == null &&
                p.OwnerId == criteria.UserId);
            return criteria.ProjectId.HasValue
                ? personal.Where(p => p.Id == criteria.ProjectId.Value)
                : personal;
        }

        var teamId = criteria.TeamId!.Value;
        var team = query.Where(p =>
            p.ProjectType == ProjectType.Team &&
            p.WorkspaceId == teamId &&
            (p.OwnerId == criteria.UserId ||
             p.Members.Any(pm => pm.UserId == criteria.UserId) ||
             _context.TeamMembers.Any(tm => tm.TeamId == teamId && tm.UserId == criteria.UserId)));
        return criteria.ProjectId.HasValue
            ? team.Where(p => p.Id == criteria.ProjectId.Value)
            : team;
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
