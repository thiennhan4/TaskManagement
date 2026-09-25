using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface ICollaborationReadRepository
{
    Task<Board?> BoardAsync(Guid id, CancellationToken ct);
    Task<PagedResult<BoardSummaryDto>> BoardsAsync(Guid user, bool admin, Guid? project, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<CommentResponseDto>> CommentsAsync(Guid task, Guid user, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ProjectMemberDto>> MembersAsync(Guid project, PageQueryDto query, CancellationToken ct);
}
