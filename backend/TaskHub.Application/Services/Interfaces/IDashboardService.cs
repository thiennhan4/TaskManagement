using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

/// <summary>
/// Dashboard data aggregation service.
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(Guid userId, DashboardQueryDto query, CancellationToken ct = default);
    Task<List<DashboardTaskDto>> GetRecentTasksAsync(Guid userId, string status = "all", int limit = 10, CancellationToken ct = default);
    Task<List<DashboardTaskDto>> GetUpcomingTasksAsync(Guid userId, int days = 7, CancellationToken ct = default);
}





