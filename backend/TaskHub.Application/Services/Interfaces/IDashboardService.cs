using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

/// <summary>
/// Dashboard data aggregation service.
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(Guid userId, DashboardQueryDto query, CancellationToken ct = default);
    Task<List<DashboardVelocityPointDto>> GetVelocityAsync(Guid userId, DashboardQueryDto query, CancellationToken ct = default);
    Task<DashboardScopeCriteria> ResolveScopeAsync(Guid userId, DashboardQueryDto query, CancellationToken ct = default);
    Task<PagedResult<DashboardTaskDto>> GetUpcomingTasksAsync(Guid userId, DashboardQueryDto query, int page = 1, int pageSize = 5, CancellationToken ct = default);
    Task<PagedResult<DashboardActivityDto>> GetActivityAsync(Guid userId, DashboardQueryDto query, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PagedResult<DashboardActivityDto>> GetProjectActivityAsync(Guid userId, Guid projectId, int page = 1, int pageSize = 5, CancellationToken ct = default);
}





