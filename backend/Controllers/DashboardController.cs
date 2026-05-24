using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Controllers;

[Route("api/dashboard")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── GET /api/dashboard/stats ───
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await _dashboardService.GetStatsAsync(GetCurrentUserId(), ct);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }

    // ─── GET /api/dashboard/recent-tasks ───
    [HttpGet("recent-tasks")]
    public async Task<IActionResult> GetRecentTasks(
        [FromQuery] string status = "all",
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var tasks = await _dashboardService.GetRecentTasksAsync(GetCurrentUserId(), status, limit, ct);
        return Ok(ApiResponse<List<DashboardTaskDto>>.Ok(tasks));
    }

    // ─── GET /api/dashboard/upcoming ───
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var tasks = await _dashboardService.GetUpcomingTasksAsync(GetCurrentUserId(), days, ct);
        return Ok(ApiResponse<List<DashboardTaskDto>>.Ok(tasks));
    }
}
