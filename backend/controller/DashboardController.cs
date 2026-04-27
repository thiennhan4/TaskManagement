using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;

namespace TaskHub.backend.controller;

[Route("api/dashboard")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── GET /api/dashboard/stats ───
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var tasks = await _context.Tasks
            .Where(t => t.OwnerId == userId)
            .Select(t => new { t.Status, t.DueDate })
            .ToListAsync(ct);

        var total = tasks.Count;
        var doing = tasks.Count(t => t.Status == "Doing");
        var done = tasks.Count(t => t.Status == "Done");
        var todo = tasks.Count(t => t.Status == "Todo");
        var overdue = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != "Done");

        return Ok(ApiResponse<object>.Ok(new { total, doing, done, todo, overdue }));
    }

    // ─── GET /api/dashboard/recent-tasks ───
    [HttpGet("recent-tasks")]
    public async Task<IActionResult> GetRecentTasks(
        [FromQuery] string status = "all",
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();

        var query = _context.Tasks
            .Where(t => t.OwnerId == userId);

        if (status != "all")
        {
            query = query.Where(t => t.Status == status);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.DueDate,
                t.Progress,
                t.ListId,
                t.CreatedAt,
                ListName = t.List.Name
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(tasks));
    }

    // ─── GET /api/dashboard/upcoming ───
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;
        var deadline = now.AddDays(days);

        var tasks = await _context.Tasks
            .Where(t => t.OwnerId == userId
                && t.Status != "Done"
                && t.DueDate.HasValue
                && t.DueDate.Value <= deadline)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.DueDate,
                t.Progress,
                ListName = t.List.Name
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(tasks));
    }
}
