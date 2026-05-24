using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.Models;
using TaskHub.backend.Repositories.Interfaces;

namespace TaskHub.backend.Repositories;

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
}
