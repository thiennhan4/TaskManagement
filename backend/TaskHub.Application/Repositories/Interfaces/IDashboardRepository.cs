using TaskHub.Application.DTOs;

namespace TaskHub.Application.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardStatsDto> GetStatsAsync(DashboardScopeCriteria criteria, CancellationToken ct = default);
}
