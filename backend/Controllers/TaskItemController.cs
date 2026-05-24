using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Controllers;

[Route("api/tasks")]
[ApiController]
[Authorize]
public class TaskItemController : ControllerBase
{
    private readonly ITaskItemService _taskService;
    private readonly ICommentService _commentService;
    private readonly INotificationService _notificationService;

    public TaskItemController(
        ITaskItemService taskService,
        ICommentService commentService,
        INotificationService notificationService)
    {
        _taskService = taskService;
        _commentService = commentService;
        _notificationService = notificationService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── GET /api/tasks ───
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] TaskFilterDto filter, CancellationToken ct)
    {
        var result = await _taskService.GetTasksAsync(filter, GetCurrentUserId(), ct);
        return Ok(ApiResponse<PagedTaskResponseDto>.Ok(result));
    }

    // ─── GET /api/tasks/{id} ───
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken ct)
    {
        var task = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskDetailResponseDto>.Ok(task));
    }

    // ─── POST /api/tasks ───
    [HttpPost]
    public async Task<IActionResult> CreatePersonalTask([FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.CreatePersonalTaskAsync(dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task created."));
    }

    // ─── POST /api/tasks/lists/{listId}/tasks ───
    [HttpPost("lists/{listId}/tasks")]
    public async Task<IActionResult> CreateTask(Guid listId, [FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.CreateTaskAsync(listId, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task created."));
    }

    // ─── PUT /api/tasks/{id} ───
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.UpdateTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task updated."));
    }

    // ─── DELETE /api/tasks/{id} ───
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        await _taskService.DeleteTaskAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Task deleted."));
    }

    // ─── PATCH /api/tasks/{id}/status ───
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeTaskStatus(Guid id, [FromBody] ChangeTaskStatusDto dto, CancellationToken ct)
    {
        var task = await _taskService.ChangeStatusAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Status updated."));
    }

    // ─── PATCH /api/tasks/{id}/assign ───
    [HttpPatch("{id}/assign")]
    public async Task<IActionResult> AssignTask(Guid id, [FromBody] AssignTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.AssignTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task assignment updated."));
    }

    // ─── PATCH /api/tasks/{id}/move ───
    [HttpPatch("{id}/move")]
    public async Task<IActionResult> MoveTask(Guid id, [FromBody] MoveTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.MoveTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task moved."));
    }

    // ─── PATCH /api/tasks/{id}/progress ───
    [HttpPatch("{id}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateProgressDto dto, CancellationToken ct)
    {
        var task = await _taskService.UpdateProgressAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Progress updated."));
    }

    // ─── COMMENTS ───
    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken ct)
    {
        var comments = await _commentService.GetCommentsByTaskAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<CommentResponseDto>>.Ok(comments));
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentDto dto, CancellationToken ct)
    {
        var comment = await _commentService.CreateCommentAsync(id, dto, GetCurrentUserId(), ct);
        
        // Trigger a test notification to the user themselves (or ideally to the task assignee)
        await _notificationService.CreateNotificationAsync(
            GetCurrentUserId(), 
            "New Comment Added", 
            $"You commented on task: {comment.Content}",
            $"/boards" // Link to the board or task
        );

        return CreatedAtAction(nameof(GetComments), new { id },
            ApiResponse<CommentResponseDto>.Ok(comment, "Comment added."));
    }

    [HttpDelete("{id}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId, CancellationToken ct)
    {
        await _commentService.DeleteCommentAsync(commentId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Comment deleted."));
    }

    // ─── ATTACHMENTS ───
    [HttpGet("{id}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid id, CancellationToken ct)
    {
        var attachments = await _taskService.GetAttachmentsAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<AttachmentResponseDto>>.Ok(attachments));
    }

    [HttpPost("{id}/attachments")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken ct)
    {
        var attachment = await _taskService.UploadAttachmentAsync(id, file, GetCurrentUserId(), ct);
        return Ok(ApiResponse<AttachmentResponseDto>.Ok(attachment, "Attachment uploaded."));
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        await _taskService.DeleteAttachmentAsync(attachmentId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Attachment deleted."));
    }

    // ─── ACTIVITY LOGS ───
    [HttpGet("{id}/activity-logs")]
    public async Task<IActionResult> GetActivityLogs(Guid id, CancellationToken ct)
    {
        var logs = await _taskService.GetActivityLogsAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<ActivityLogResponseDto>>.Ok(logs));
    }

    // ─── GET /api/tasks/my-tasks ───
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks(CancellationToken ct)
    {
        var tasks = await _taskService.GetMyTasksAsync(GetCurrentUserId(), ct);
        return Ok(ApiResponse<List<TaskResponseDto>>.Ok(tasks));
    }

    // ─── GET /api/tasks/calendar ───
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendarTasks([FromQuery] CalendarFilterDto filter, CancellationToken ct)
    {
        var tasks = await _taskService.GetCalendarTasksAsync(filter, GetCurrentUserId(), ct);
        return Ok(ApiResponse<List<TaskCalendarDto>>.Ok(tasks));
    }

    // ─── POST /api/tasks/{id}/invite ───
    [HttpPost("{id}/invite")]
    public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteTaskMemberDto dto, CancellationToken ct)
    {
        await _taskService.InviteMemberToTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, $"Invitation process completed for {dto.Email}."));
    }

    // ─── POST /api/tasks/accept-invite ───
    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptTaskInvitationDto dto, CancellationToken ct)
    {
        await _taskService.AcceptTaskInvitationAsync(dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Invitation accepted. You are now assigned to the task."));
    }
}
