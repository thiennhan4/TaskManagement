using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;
using TaskHub.backend.Exceptions;
using TaskHub.backend.Models;
using TaskHub.backend.Repositories.Interfaces;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly AppDbContext _context;

    public CommentService(
        ICommentRepository commentRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        AppDbContext context)
    {
        _commentRepository = commentRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _context = context;
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

        return new CommentResponseDto
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
    }
}
