using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _service;

    public AnalyticsController(IAnalyticsService service)
    {
        _service = service;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get analytics overview with velocity, burndown, distributions.</summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] DashboardQueryDto scope, [FromQuery] Guid? boardId, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await _service.GetOverviewAsync(GetUserId(), scope, boardId, days, ct);
        return Ok(ApiResponse<AnalyticsOverviewDto>.Ok(result));
    }
}
