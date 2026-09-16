using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Data;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;

namespace TaskHub.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _context;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IPermissionService _permissionService;

    public DashboardService(
        IAppDbContext context,
        IDashboardRepository dashboardRepository,
        ITeamRepository teamRepository,
        IPermissionService permissionService)
    {
        _context = context;
        _dashboardRepository = dashboardRepository;
        _teamRepository = teamRepository;
        _permissionService = permissionService;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(Guid userId, DashboardQueryDto query, CancellationToken ct = default)
    {
        var criteria = await ResolveScopeAsync(userId, query, ct);
        return await _dashboardRepository.GetStatsAsync(criteria, ct);
    }

    public async Task<List<DashboardVelocityPointDto>> GetVelocityAsync(Guid userId, DashboardQueryDto query, CancellationToken ct = default)
    {
        var criteria = await ResolveScopeAsync(userId, query, ct);
        var rawTimeframe = string.IsNullOrWhiteSpace(query.Timeframe)
            ? nameof(DashboardTimeframe.SixMonths)
            : query.Timeframe;
        if (!Enum.TryParse<DashboardTimeframe>(rawTimeframe, true, out var timeframe) ||
            !Enum.GetNames<DashboardTimeframe>().Any(name => name.Equals(rawTimeframe, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("Dashboard timeframe must be Week, Month, SixMonths, or Year.");
        }

        return await _dashboardRepository.GetVelocityAsync(criteria, timeframe, DateTime.UtcNow, ct);
    }

    public async Task<DashboardScopeCriteria> ResolveScopeAsync(Guid userId, DashboardQueryDto? query, CancellationToken ct = default)
    {
        query ??= new DashboardQueryDto();
        var rawScope = string.IsNullOrWhiteSpace(query.Scope) ? nameof(DashboardScope.Personal) : query.Scope;

        if (!Enum.TryParse<DashboardScope>(rawScope, ignoreCase: true, out var scope) ||
            !Enum.IsDefined(scope) ||
            !Enum.GetNames<DashboardScope>().Any(name => name.Equals(rawScope, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("Dashboard scope must be Personal or Team.");
        }

        if (scope == DashboardScope.Personal)
        {
            if (query.TeamId.HasValue)
            {
                throw new BadRequestException("Personal dashboard scope does not accept teamId.");
            }

            return new DashboardScopeCriteria
            {
                UserId = userId,
                Scope = DashboardScope.Personal
            };
        }

        if (!query.TeamId.HasValue)
        {
            throw new BadRequestException("Team dashboard scope requires teamId.");
        }

        _ = await _teamRepository.GetTeamByIdAsync(query.TeamId.Value, ct)
            ?? throw new NotFoundException("Team", query.TeamId.Value);

        await _permissionService.AuthorizeTeamActionAsync(userId, query.TeamId.Value, TeamAction.View, ct);

        return new DashboardScopeCriteria
        {
            UserId = userId,
            Scope = DashboardScope.Team,
            TeamId = query.TeamId
        };
    }

    public async Task<List<DashboardTaskDto>> GetRecentTasksAsync(Guid userId, string status = "all", int limit = 10, CancellationToken ct = default)
    {
        var query = _context.Tasks
            .Where(t => t.OwnerId == userId);

        if (status != "all" && Enum.TryParse<TaskItemStatus>(status, true, out var parsedStatus))
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
                && t.Status != TaskItemStatus.Done
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





