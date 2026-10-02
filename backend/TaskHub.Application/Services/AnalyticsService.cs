using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;

namespace TaskHub.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IDashboardService _dashboardService;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IPermissionService _permissionService;

    public AnalyticsService(
        IDashboardService dashboardService,
        IDashboardRepository dashboardRepository,
        IBoardRepository boardRepository,
        IPermissionService permissionService)
    {
        _dashboardService = dashboardService;
        _dashboardRepository = dashboardRepository;
        _boardRepository = boardRepository;
        _permissionService = permissionService;
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(
        Guid userId, DashboardQueryDto scope, Guid? boardId = null, int days = 30, CancellationToken ct = default)
    {
        if (days is < 1 or > 365)
        {
            throw new BadRequestException("days must be between 1 and 365.");
        }

        var criteria = await _dashboardService.ResolveScopeAsync(userId, scope, ct);
        if (boardId.HasValue)
        {
            var board = await _boardRepository.GetBoardByIdAsync(boardId.Value, ct)
                ?? throw new NotFoundException("Board", boardId.Value);
            await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);
        }

        var now = DateTime.UtcNow;
        var from = now.AddDays(-days);
        return await _dashboardRepository.GetAnalyticsAsync(criteria, boardId, from, now, ct);
    }
}
