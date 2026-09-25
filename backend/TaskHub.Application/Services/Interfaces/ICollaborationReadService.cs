using TaskHub.Application.DTOs;
namespace TaskHub.Application.Services.Interfaces;

public interface ICollaborationReadService
{
    Task<PagedResult<BoardSummaryDto>> BoardsAsync(Guid user, Guid? project, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<KanbanColumnDto>> ColumnsAsync(Guid user, Guid board, KanbanQueryDto query, CancellationToken ct);
    Task<PagedResult<CommentResponseDto>> CommentsAsync(Guid user, Guid task, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ProjectMemberDto>> MembersAsync(Guid user, Guid project, PageQueryDto query, CancellationToken ct);
}
