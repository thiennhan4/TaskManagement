using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[Route("api/dashboard")]
[Route("api/v1/dashboard")]
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
    public async Task<IActionResult> GetStats([FromQuery] DashboardQueryDto query, CancellationToken ct)
    {
        var stats = await _dashboardService.GetStatsAsync(GetCurrentUserId(), query, ct);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }

    [HttpGet("velocity")]
    public async Task<IActionResult> GetVelocity([FromQuery] DashboardQueryDto query, CancellationToken ct)
    {
        var velocity = await _dashboardService.GetVelocityAsync(GetCurrentUserId(), query, ct);
        return Ok(ApiResponse<List<DashboardVelocityPointDto>>.Ok(velocity));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] DashboardQueryDto query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _dashboardService.GetActivityAsync(GetCurrentUserId(), query, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<DashboardActivityDto>>.Ok(result));
    }

    [HttpGet("/api/v1/projects/{projectId}/activity")]
    public async Task<IActionResult> GetProjectActivity(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, CancellationToken ct = default)
    {
        var result = await _dashboardService.GetProjectActivityAsync(GetCurrentUserId(), projectId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<DashboardActivityDto>>.Ok(result));
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] DashboardQueryDto query, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, CancellationToken ct = default)
    {
        var tasks = await _dashboardService.GetUpcomingTasksAsync(GetCurrentUserId(), query, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<DashboardTaskDto>>.Ok(tasks));
    }
}



