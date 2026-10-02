using TaskHub.Application.DTOs;
namespace TaskHub.Application.Services.Interfaces;

public interface ICollaborationReadService
{
    Task<PagedResult<ProjectResponseDto>> ProjectsAsync(Guid user, ProjectQueryDto query, CancellationToken ct);
    Task<ProjectResponseDto> ProjectAsync(Guid user, Guid id, CancellationToken ct);
    Task<PagedResult<TeamResponseDto>> TeamsAsync(Guid user, PageQueryDto query, CancellationToken ct);
    Task<TeamResponseDto> TeamAsync(Guid user, Guid id, CancellationToken ct);
    Task<PagedResult<TeamMemberResponseDto>> TeamMembersAsync(Guid user, Guid team, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<AttachmentResponseDto>> AttachmentsAsync(Guid user, Guid task, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ActivityLogResponseDto>> TaskActivityAsync(Guid user, Guid task, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ProjectActivityResponseDto>> ProjectActivityAsync(Guid user, Guid project, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<BoardSummaryDto>> BoardsAsync(Guid user, Guid? project, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<KanbanColumnDto>> ColumnsAsync(Guid user, Guid board, KanbanQueryDto query, CancellationToken ct);
    Task<PagedResult<CommentResponseDto>> CommentsAsync(Guid user, Guid task, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ProjectMemberDto>> MembersAsync(Guid user, Guid project, PageQueryDto query, CancellationToken ct);
}
