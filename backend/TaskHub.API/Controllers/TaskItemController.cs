using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

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

    // â”€â”€â”€ GET /api/tasks â”€â”€â”€
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] TaskFilterDto filter, CancellationToken ct)
    {
        var result = await _taskService.GetTasksAsync(filter, GetCurrentUserId(), ct);
        return Ok(ApiResponse<PagedTaskResponseDto>.Ok(result));
    }

    // â”€â”€â”€ GET /api/tasks/{id} â”€â”€â”€
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken ct)
    {
        var task = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskDetailResponseDto>.Ok(task));
    }

    // â”€â”€â”€ POST /api/tasks â”€â”€â”€
    [HttpPost]
    public async Task<IActionResult> CreatePersonalTask([FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.CreatePersonalTaskAsync(dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task created."));
    }

    // â”€â”€â”€ POST /api/tasks/lists/{listId}/tasks â”€â”€â”€
    [HttpPost("lists/{listId}/tasks")]
    public async Task<IActionResult> CreateTask(Guid listId, [FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.CreateTaskAsync(listId, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task created."));
    }

    // â”€â”€â”€ PUT /api/tasks/{id} â”€â”€â”€
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.UpdateTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task updated."));
    }

    // â”€â”€â”€ DELETE /api/tasks/{id} â”€â”€â”€
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        await _taskService.DeleteTaskAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Task deleted."));
    }

    // â”€â”€â”€ PATCH /api/tasks/{id}/status â”€â”€â”€
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeTaskStatus(Guid id, [FromBody] ChangeTaskStatusDto dto, CancellationToken ct)
    {
        var task = await _taskService.ChangeStatusAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Status updated."));
    }

    // â”€â”€â”€ PATCH /api/tasks/{id}/assign â”€â”€â”€
    [HttpPatch("{id}/assign")]
    public async Task<IActionResult> AssignTask(Guid id, [FromBody] AssignTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.AssignTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task assignment updated."));
    }

    // â”€â”€â”€ PATCH /api/tasks/{id}/move â”€â”€â”€
    [HttpPatch("{id}/move")]
    public async Task<IActionResult> MoveTask(Guid id, [FromBody] MoveTaskDto dto, CancellationToken ct)
    {
        var task = await _taskService.MoveTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Task moved."));
    }

    // â”€â”€â”€ PATCH /api/tasks/{id}/progress â”€â”€â”€
    [HttpPatch("{id}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateProgressDto dto, CancellationToken ct)
    {
        var task = await _taskService.UpdateProgressAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TaskResponseDto>.Ok(task, "Progress updated."));
    }

    // â”€â”€â”€ COMMENTS â”€â”€â”€
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

    // â”€â”€â”€ ATTACHMENTS â”€â”€â”€
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

    // â”€â”€â”€ ACTIVITY LOGS â”€â”€â”€
    [HttpGet("{id}/activity-logs")]
    public async Task<IActionResult> GetActivityLogs(Guid id, CancellationToken ct)
    {
        var logs = await _taskService.GetActivityLogsAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<ActivityLogResponseDto>>.Ok(logs));
    }

    // â”€â”€â”€ GET /api/tasks/my-tasks â”€â”€â”€
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks(CancellationToken ct)
    {
        var tasks = await _taskService.GetMyTasksAsync(GetCurrentUserId(), ct);
        return Ok(ApiResponse<List<TaskResponseDto>>.Ok(tasks));
    }

    // â”€â”€â”€ GET /api/tasks/calendar â”€â”€â”€
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendarTasks([FromQuery] CalendarFilterDto filter, CancellationToken ct)
    {
        var tasks = await _taskService.GetCalendarTasksAsync(filter, GetCurrentUserId(), ct);
        return Ok(ApiResponse<List<TaskCalendarDto>>.Ok(tasks));
    }

    // â”€â”€â”€ POST /api/tasks/{id}/invite â”€â”€â”€
    [HttpPost("{id}/invite")]
    public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteTaskMemberDto dto, CancellationToken ct)
    {
        await _taskService.InviteMemberToTaskAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, $"Invitation process completed for {dto.Email}."));
    }

    // â”€â”€â”€ POST /api/tasks/accept-invite â”€â”€â”€
    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptTaskInvitationDto dto, CancellationToken ct)
    {
        await _taskService.AcceptTaskInvitationAsync(dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Invitation accepted. You are now assigned to the task."));
    }
}



