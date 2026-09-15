using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Data;
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
    private readonly ICommentRepository _commentRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly IAppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CommentService(
        ICommentRepository commentRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        IAppDbContext context,
        IHubContext<NotificationHub> hubContext)
    {
        _commentRepository = commentRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<IEnumerable<CommentResponseDto>> GetCommentsByTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
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

    public async Task<CommentResponseDto> CreateCommentAsync(Guid taskId, CreateCommentDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);

        // Viewing a task allows commenting on it
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.View, ct);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        var comment = new Comment
        {
            Content = dto.Content,
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.CreateCommentAsync(comment, ct);
        await _auditService.LogAsync(userId, "CreateComment", "Task", taskId, null, ct);

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
        await _hubContext.Clients.Group($"task_{taskId}").SendAsync("CommentAdded", commentDto);

        // Broadcast comments count update to board room
        var taskWithRelations = await _context.Tasks.Include(t => t.List).FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (taskWithRelations?.List != null)
        {
            var commentsCount = await _context.Comments.CountAsync(c => c.TaskId == taskId, ct);
            await _hubContext.Clients.Group($"board_{taskWithRelations.List.BoardId}").SendAsync("TaskCommentsCountUpdated", new { taskId, count = commentsCount });
        }

        return commentDto;
    }

    public async Task DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default)
    {
        var comment = await _commentRepository.GetCommentByIdAsync(commentId, ct)
            ?? throw new NotFoundException("Comment", commentId);

        // Only comment author or system admin can delete comment
        if (comment.UserId != userId && !await _permissionService.IsAdminAsync(userId, ct))
        {
            throw new ForbiddenException("You can only delete your own comments.");
        }

        await _commentRepository.DeleteCommentAsync(commentId, ct);
        await _auditService.LogAsync(userId, "DeleteComment", "Task", comment.TaskId, new { commentId }, ct);

        // Broadcast comment deleted to task room
        await _hubContext.Clients.Group($"task_{comment.TaskId}").SendAsync("CommentDeleted", commentId);

        // Broadcast comments count update to board room
        var taskWithRelations = await _context.Tasks.Include(t => t.List).FirstOrDefaultAsync(t => t.Id == comment.TaskId, ct);
        if (taskWithRelations?.List != null)
        {
            var commentsCount = await _context.Comments.CountAsync(c => c.TaskId == comment.TaskId, ct);
            await _hubContext.Clients.Group($"board_{taskWithRelations.List.BoardId}").SendAsync("TaskCommentsCountUpdated", new { taskId = comment.TaskId, count = commentsCount });
        }
    }
}





