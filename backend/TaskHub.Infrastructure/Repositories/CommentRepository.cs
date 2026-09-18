using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;

namespace TaskHub.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _context;

    public CommentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Comment>> GetCommentsByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Comment?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);
    }

    public async Task<Comment> CreateCommentAsync(Comment comment, CancellationToken ct = default)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(ct);
        return comment;
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        var comment = await _context.Comments.FindAsync(new object[] { commentId }, ct);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task AddActivityLogAsync(TaskActivityLog log, CancellationToken ct = default)
    {
        _context.TaskActivityLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    public Task<TaskItem?> GetTaskForAuthorizationAsync(Guid taskId, CancellationToken ct = default) =>
        _context.Tasks.Include(task => task.List).ThenInclude(list => list.Board)
            .ThenInclude(board => board.Project)
            .FirstOrDefaultAsync(task => task.Id == taskId && !task.IsDeleted, ct);

    public Task<AppUser?> GetUserAsync(Guid userId, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(user => user.Id == userId, ct);

    public Task<int> GetCommentsCountAsync(Guid taskId, CancellationToken ct = default) =>
        _context.Comments.CountAsync(comment => comment.TaskId == taskId && !comment.IsDeleted, ct);
}




