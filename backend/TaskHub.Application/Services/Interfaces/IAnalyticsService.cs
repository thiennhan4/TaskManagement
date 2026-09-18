using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(Guid userId, DashboardQueryDto scope, Guid? boardId = null, int days = 30, CancellationToken ct = default);
}
