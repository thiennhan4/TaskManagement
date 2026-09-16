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
