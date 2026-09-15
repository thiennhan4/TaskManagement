using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

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

    // â”€â”€â”€ GET /api/dashboard/stats â”€â”€â”€
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await _dashboardService.GetStatsAsync(GetCurrentUserId(), ct);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }

    // â”€â”€â”€ GET /api/dashboard/recent-tasks â”€â”€â”€
    [HttpGet("recent-tasks")]
    public async Task<IActionResult> GetRecentTasks(
        [FromQuery] string status = "all",
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var tasks = await _dashboardService.GetRecentTasksAsync(GetCurrentUserId(), status, limit, ct);
        return Ok(ApiResponse<List<DashboardTaskDto>>.Ok(tasks));
    }

    // â”€â”€â”€ GET /api/dashboard/upcoming â”€â”€â”€
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var tasks = await _dashboardService.GetUpcomingTasksAsync(GetCurrentUserId(), days, ct);
        return Ok(ApiResponse<List<DashboardTaskDto>>.Ok(tasks));
    }
}



