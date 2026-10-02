using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimeTrackingController : ControllerBase
{
    private readonly ITimeTrackingService _service;

    public TimeTrackingController(ITimeTrackingService service)
    {
        _service = service;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("/api/v1/timetracking/my-entries")]
    public async Task<IActionResult> UserPage([FromQuery] TimeQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<TimeEntryDto>>.Ok(await _service.GetUserPageAsync(GetUserId(), query, ct)));

    [HttpGet("/api/v1/timetracking/report")]
    public async Task<IActionResult> ReportPage([FromQuery] TimeQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<TimeReportDto>.Ok(await _service.GetReportPageAsync(GetUserId(), query, ct)));

    /// <summary>Start a timer for a task.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartTimer([FromBody] StartTimerDto dto, CancellationToken ct = default)
    {
        var result = await _service.StartTimerAsync(GetUserId(), dto, ct: ct);
        return Ok(ApiResponse<object?>.Ok(result));
    }

    /// <summary>Stop a running timer.</summary>
    [HttpPost("{entryId}/stop")]
    public async Task<IActionResult> StopTimer(Guid entryId, [FromBody] StopTimerDto? dto = null, CancellationToken ct = default)
    {
        var result = await _service.StopTimerAsync(GetUserId(), entryId, dto, ct: ct);
        return Ok(ApiResponse<object?>.Ok(result));
    }

    /// <summary>Get the currently running timer for the authenticated user.</summary>
    [HttpGet("running")]
    public async Task<IActionResult> GetRunningTimer(CancellationToken ct = default)
    {
        var result = await _service.GetRunningTimerAsync(GetUserId(), ct: ct);
        return Ok(ApiResponse<object?>.Ok(result));
    }

    /// <summary>Create a manual time entry.</summary>
    [HttpPost("manual")]
    public async Task<IActionResult> CreateManualEntry([FromBody] ManualTimeEntryDto dto, CancellationToken ct = default)
    {
        var result = await _service.CreateManualEntryAsync(GetUserId(), dto, ct: ct);
        return Ok(ApiResponse<object?>.Ok(result));
    }

    /// <summary>Delete a time entry.</summary>
    [HttpDelete("{entryId}")]
    public async Task<IActionResult> DeleteEntry(Guid entryId, CancellationToken ct = default)
    {
        await _service.DeleteEntryAsync(GetUserId(), entryId, ct: ct);
        return Ok(ApiResponse<object>.Ok(null!, "Time entry deleted."));
    }

    /// <summary>Get time entries for a specific task.</summary>
    [HttpGet("/api/v1/timetracking/task/{taskId}")]
    [HttpGet("task/{taskId}")]
    public async Task<IActionResult> GetEntriesForTask(Guid taskId, [FromQuery] PageQueryDto query, CancellationToken ct = default)
    {
        var result = await _service.GetEntriesForTaskAsync(taskId, GetUserId(), query, ct: ct);
        return Ok(ApiResponse<object?>.Ok(result));
    }

    /// <summary>Get time entries for the authenticated user.</summary>
    [HttpGet("my-entries")]
    public async Task<IActionResult> GetMyEntries([FromQuery] TimeQueryDto query, CancellationToken ct = default)
    {
        var result = await _service.GetUserPageAsync(GetUserId(), query, ct);
        return this.LegacyPage(result);
    }

    /// <summary>Get a time tracking report.</summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetReport([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? boardId = null, CancellationToken ct = default)
    {
        var result = await _service.GetReportAsync(GetUserId(), from, to, boardId, ct: ct);
        return Ok(ApiResponse<object?>.Ok(result));
    }
}
