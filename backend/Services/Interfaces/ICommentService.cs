using TaskHub.backend.DTOs;

namespace TaskHub.backend.Services.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentResponseDto>> GetCommentsByTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<CommentResponseDto> CreateCommentAsync(Guid taskId, CreateCommentDto dto, Guid userId, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default);
}
