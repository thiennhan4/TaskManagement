using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetCommentsByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<Comment?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default);
    Task<Comment> CreateCommentAsync(Comment comment, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default);
}




