using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var tasks = await _context.Tasks
            .Where(t => t.OwnerId == userId && !t.IsDeleted)
            .Select(t => new { t.Status, t.DueDate })
            .ToListAsync(ct);

        var totalBoards = await _context.Boards
            .CountAsync(b => b.OwnerId == userId, ct);

        // Count unique team members across all teams user is in
        var userTeamIds = await _context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TeamId)
            .ToListAsync(ct);

        var teamMembersCount = await _context.TeamMembers
            .Where(tm => userTeamIds.Contains(tm.TeamId))
            .Select(tm => tm.UserId)
            .Distinct()
            .CountAsync(ct);

        return new DashboardStatsDto
        {
            Total = tasks.Count,
            Todo = tasks.Count(t => t.Status == Models.TaskItemStatus.Todo),
            InProgress = tasks.Count(t => t.Status == Models.TaskItemStatus.InProgress),
            Done = tasks.Count(t => t.Status == Models.TaskItemStatus.Done),
            Overdue = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != Models.TaskItemStatus.Done),
            TotalBoards = totalBoards,
            TeamMembers = teamMembersCount
        };
    }

    public async Task<List<DashboardTaskDto>> GetRecentTasksAsync(Guid userId, string status = "all", int limit = 10, CancellationToken ct = default)
    {
        var query = _context.Tasks
            .Where(t => t.OwnerId == userId);

        if (status != "all" && Enum.TryParse<Models.TaskItemStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(t => t.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new DashboardTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                DueDate = t.DueDate,
                Progress = t.Progress,
                ListId = t.ListId,
                ListName = t.List.Name,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<List<DashboardTaskDto>> GetUpcomingTasksAsync(Guid userId, int days = 7, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var deadline = now.AddDays(days);

        return await _context.Tasks
            .Where(t => t.OwnerId == userId
                && t.Status != Models.TaskItemStatus.Done
                && t.DueDate.HasValue
                && t.DueDate.Value <= deadline)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => new DashboardTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                DueDate = t.DueDate,
                Progress = t.Progress,
                ListId = t.ListId,
                ListName = t.List.Name,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);
    }
}
