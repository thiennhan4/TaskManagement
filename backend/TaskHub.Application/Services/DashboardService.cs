using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPermissionService _permissionService;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        ITeamRepository teamRepository,
        IProjectRepository projectRepository,
        IPermissionService permissionService)
    {
        _dashboardRepository = dashboardRepository;
        _teamRepository = teamRepository;
        _projectRepository = projectRepository;
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

    public async Task<PagedResult<DashboardActivityDto>> GetActivityAsync(Guid userId, DashboardQueryDto query, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1 || page > 100 || pageSize < 1 || pageSize > 50)
            throw new BadRequestException("Activity page must be 1-100 and pageSize must be 1-50.");

        var criteria = await ResolveScopeAsync(userId, query, ct);
        return await _dashboardRepository.GetActivityAsync(criteria, page, pageSize, ct);
    }

    public async Task<PagedResult<DashboardActivityDto>> GetProjectActivityAsync(Guid userId, Guid projectId, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        if (page < 1 || page > 100 || pageSize < 1 || pageSize > 50)
            throw new BadRequestException("Activity page must be 1-100 and pageSize must be 1-50.");

        var project = await _projectRepository.GetByIdAsync(projectId, ct)
            ?? throw new NotFoundException("Project", projectId);
        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.View, ct);
        var criteria = new DashboardScopeCriteria
        {
            UserId = userId,
            Scope = project.ProjectType == ProjectType.Personal ? DashboardScope.Personal : DashboardScope.Team,
            TeamId = project.WorkspaceId,
            ProjectId = projectId
        };
        return await _dashboardRepository.GetActivityAsync(criteria, page, pageSize, ct);
    }

    public async Task<PagedResult<DashboardTaskDto>> GetUpcomingTasksAsync(Guid userId, DashboardQueryDto query, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        if (page < 1 || page > 100 || pageSize < 1 || pageSize > 50)
            throw new BadRequestException("Upcoming page must be 1-100 and pageSize must be 1-50.");

        var criteria = await ResolveScopeAsync(userId, query, ct);
        var fromUtc = DateTime.UtcNow.Date;
        return await _dashboardRepository.GetUpcomingAsync(criteria, fromUtc, fromUtc.AddDays(8), page, pageSize, ct);
    }
}





