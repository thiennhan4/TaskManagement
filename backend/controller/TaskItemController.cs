using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;
using TaskHub.backend.Models;

namespace TaskHub.backend.controller;

[Route("api/tasks")]
[ApiController]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _context;

    public TaskController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── GET /api/tasks/lists/{listId}/tasks ───
    [HttpGet("lists/{listId}/tasks")]
    public async Task<IActionResult> GetTasksByList(Guid listId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var list = await _context.Lists
            .Include(l => l.Board)
            .FirstOrDefaultAsync(l => l.Id == listId, ct);

        if (list == null || list.Board.OwnerId != userId)
            return NotFound(ApiResponse<object>.Fail("List not found or access denied."));

        var tasks = await _context.Tasks
            .Where(t => t.ListId == listId)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(tasks));
    }

    // ─── POST /api/tasks/lists/{listId}/tasks ───
    [HttpPost("lists/{listId}/tasks")]
    public async Task<IActionResult> CreateTask(Guid listId, [FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var list = await _context.Lists
            .Include(l => l.Board)
            .FirstOrDefaultAsync(l => l.Id == listId, ct);

        if (list == null || list.Board.OwnerId != userId)
            return BadRequest(ApiResponse<object>.Fail("List not found or access denied."));

        var maxPos = await _context.Tasks
            .Where(t => t.ListId == listId)
            .MaxAsync(t => (int?)t.Position, ct) ?? -1;

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            ListId = listId,
            OwnerId = userId,
            Position = maxPos + 1,
            Status = list.Name == "Done" ? "Done" : list.Name == "Doing" ? "Doing" : "Todo",
            Priority = dto.Priority ?? "Medium",
            DueDate = dto.DueDate,
            Label = dto.Label,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);

        await LogAudit(userId, "CreateTask", "Task", task.Id,
            new { task.Title, task.ListId }, ct);

        return Ok(ApiResponse<object>.Ok(task, "Task created."));
    }

    // ─── PUT /api/tasks/{id} ───
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var task = await _context.Tasks.FindAsync(new object[] { id }, ct);

        if (task == null || task.OwnerId != userId)
            return NotFound(ApiResponse<object>.Fail("Task not found or access denied."));

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.DueDate = dto.DueDate;
        task.Label = dto.Label;
        task.Progress = dto.Progress;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await LogAudit(userId, "UpdateTask", "Task", task.Id,
            new { task.Title, task.Status }, ct);

        return Ok(ApiResponse<object>.Ok(task, "Task updated."));
    }

    // ─── DELETE /api/tasks/{id} ───
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var task = await _context.Tasks.FindAsync(new object[] { id }, ct);

        if (task == null || task.OwnerId != userId)
            return NotFound(ApiResponse<object>.Fail("Task not found or access denied."));

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(ct);

        await LogAudit(userId, "DeleteTask", "Task", id,
            new { task.Title }, ct);

        return Ok(ApiResponse<object>.Ok(null!, "Task deleted."));
    }

    // ─── PATCH /api/tasks/{id}/move ───
    [HttpPatch("{id}/move")]
    public async Task<IActionResult> MoveTask(Guid id, [FromBody] MoveTaskDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var task = await _context.Tasks.FindAsync(new object[] { id }, ct);

        if (task == null || task.OwnerId != userId)
            return NotFound(ApiResponse<object>.Fail("Task not found or access denied."));

        var targetList = await _context.Lists
            .Include(l => l.Board)
            .FirstOrDefaultAsync(l => l.Id == dto.ListId, ct);

        if (targetList == null || targetList.Board.OwnerId != userId)
            return BadRequest(ApiResponse<object>.Fail("Target list is invalid."));

        task.ListId = dto.ListId;
        task.Position = dto.Position;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await LogAudit(userId, "MoveTask", "Task", task.Id,
            new { task.Title, dto.ListId, dto.Position }, ct);

        return Ok(ApiResponse<object>.Ok(task, "Task moved."));
    }

    // ─── PATCH /api/tasks/{id}/progress ───
    [HttpPatch("{id}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateProgressDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var task = await _context.Tasks.FindAsync(new object[] { id }, ct);

        if (task == null || task.OwnerId != userId)
            return NotFound(ApiResponse<object>.Fail("Task not found or access denied."));

        task.Progress = dto.Progress;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(task, "Progress updated."));
    }

    // ═══ PRIVATE HELPERS ═══

    private async Task LogAudit(Guid userId, string action, string entityType, Guid? entityId, object? detail, CancellationToken ct)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail != null ? JsonSerializer.Serialize(detail) : null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(ct);
    }
}
