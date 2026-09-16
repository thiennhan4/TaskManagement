using TaskHub.Application.DTOs;

namespace TaskHub.Application.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardStatsDto> GetStatsAsync(DashboardScopeCriteria criteria, CancellationToken ct = default);
    Task<List<DashboardVelocityPointDto>> GetVelocityAsync(DashboardScopeCriteria criteria, DashboardTimeframe timeframe, DateTime asOfUtc, CancellationToken ct = default);
    Task<DashboardAnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(DashboardScopeCriteria criteria, Guid? boardId, DateTime from, DateTime to, CancellationToken ct = default);
}
