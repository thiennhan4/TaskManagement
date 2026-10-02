using TaskHub.Application.DTOs;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using TaskHub.Application.Hubs;

namespace TaskHub.Application.Services;

public class CommentService : ICommentService
{
    private readonly IMutationRunner _mutations;
    private readonly DurableDelivery _delivery;
    private readonly ICommentRepository _commentRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly IProtectedHubContext _hubContext;

    public CommentService(
        ICommentRepository commentRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        IProtectedHubContext hubContext, IMutationRunner mutations, DurableDelivery delivery)
    {
        _mutations = mutations;
        _delivery = delivery;
        _commentRepository = commentRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _hubContext = hubContext;
    }

    public async Task<IEnumerable<CommentResponseDto>> GetCommentsByTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await _commentRepository.GetTaskForAuthorizationAsync(taskId, ct)
            ?? throw new NotFoundException("Task", taskId);

        // Can only read comments if you have access to read the task
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.View, ct);

        var comments = await _commentRepository.GetCommentsByTaskIdAsync(taskId, ct);

        return comments.Select(c => new CommentResponseDto
        {
            Id = c.Id,
            Content = c.Content,
            UserId = c.UserId,
            UserName = c.User.FullName,
            UserAvatar = c.User.AvatarUrl,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            IsOwner = c.UserId == userId
        });
    }

    public Task<CommentResponseDto> CreateCommentAsync(Guid taskId, CreateCommentDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => CreateCommentAsyncCore(taskId, dto, userId, ct), ct);

    private async Task<CommentResponseDto> CreateCommentAsyncCore(Guid taskId, CreateCommentDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await _commentRepository.GetTaskForAuthorizationAsync(taskId, ct)
            ?? throw new NotFoundException("Task", taskId);

        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);

        var user = await _commentRepository.GetUserAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        var comment = new Comment
        {
            Content = dto.Content,
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.CreateCommentAsync(comment, ct);
        var activity = new TaskActivityLog
        {
            TaskId = taskId,
            UserId = userId,
            Action = ActivityLogAction.Commented,
            CreatedAt = comment.CreatedAt
        };
        await _commentRepository.AddActivityLogAsync(activity, ct);
        await _auditService.LogAsync(userId, "CreateComment", "Task", taskId, null, ct);

        if (task.List?.Board?.Project is Project project)
        {
            var payload = new ProjectActivityEventDto
            {
                ProjectId = project.Id,
                EventType = "CommentAdded",
                CreatedAt = activity.CreatedAt
            };
            await _delivery.EnqueueAsync("Realtime", new { Group = $"project_{project.Id}", Event = "ProjectActivity", Arguments = new object?[] {  payload } }, () => _hubContext.Clients.Group($"project_{project.Id}").SendAsync("ProjectActivity", payload, ct), ct);
            if (project.WorkspaceId is Guid teamId)
                await _delivery.EnqueueAsync("Realtime", new { Group = $"team_{teamId}", Event = "ProjectActivity", Arguments = new object?[] {  payload } }, () => _hubContext.Clients.Group($"team_{teamId}").SendAsync("ProjectActivity", payload, ct), ct);
        }

        var commentDto = new CommentResponseDto
        {
            Id = comment.Id,
            Content = comment.Content,
            UserId = comment.UserId,
            UserName = user!.FullName,
            UserAvatar = user.AvatarUrl,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsOwner = true
        };

        // Broadcast comment added to task room
        await _delivery.EnqueueAsync("Realtime", new { Group = $"task_{taskId}", Event = "CommentAdded", Arguments = new object?[] {  commentDto } }, () => _hubContext.Clients.Group($"task_{taskId}").SendAsync("CommentAdded", commentDto), ct);

        // Broadcast comments count update to board room
        if (task.List != null)
        {
            var commentsCount = await _commentRepository.GetCommentsCountAsync(taskId, ct);
            await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{task.List.BoardId}", Event = "TaskCommentsCountUpdated", Arguments = new object?[] {  new { taskId, count = commentsCount } } }, () => _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskCommentsCountUpdated", new { taskId, count = commentsCount }), ct);
        }

        return commentDto;
    }

    public Task DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default, Guid? taskId = null) =>
        _mutations.RunAsync(() => DeleteCommentAsyncCore(commentId, userId, ct, taskId), ct);

    private async Task DeleteCommentAsyncCore(Guid commentId, Guid userId, CancellationToken ct = default, Guid? taskId = null)
    {
        var comment = await _commentRepository.GetCommentByIdAsync(commentId, ct)
            ?? throw new NotFoundException("Comment", commentId);

        if (taskId.HasValue && comment.TaskId != taskId.Value) throw new NotFoundException("Comment", commentId);

        // Only comment author or system admin can delete comment
        if (comment.UserId != userId && !await _permissionService.IsAdminAsync(userId, ct))
        {
            throw new ForbiddenException("You can only delete your own comments.");
        }

        var task = await _commentRepository.GetTaskForAuthorizationAsync(comment.TaskId, ct)
            ?? throw new NotFoundException("Task", comment.TaskId);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);

        await _commentRepository.DeleteCommentAsync(commentId, ct);
        await _auditService.LogAsync(userId, "DeleteComment", "Task", comment.TaskId, new { commentId }, ct);

        // Broadcast comment deleted to task room
        await _delivery.EnqueueAsync("Realtime", new { Group = $"task_{comment.TaskId}", Event = "CommentDeleted", Arguments = new object?[] {  commentId } }, () => _hubContext.Clients.Group($"task_{comment.TaskId}").SendAsync("CommentDeleted", commentId), ct);

        // Broadcast comments count update to board room
        if (task.List != null)
        {
            var commentsCount = await _commentRepository.GetCommentsCountAsync(comment.TaskId, ct);
            await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{task.List.BoardId}", Event = "TaskCommentsCountUpdated", Arguments = new object?[] {  new { taskId = comment.TaskId, count = commentsCount } } }, () => _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskCommentsCountUpdated", new { taskId = comment.TaskId, count = commentsCount }), ct);
        }
    }
}





