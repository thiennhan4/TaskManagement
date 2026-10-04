using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface ICollaborationReadRepository
{

    Task<PagedResult<ProjectResponseDto>> ProjectsAsync(Guid user, bool admin, ProjectQueryDto query, CancellationToken ct);
    Task<ProjectResponseDto?> ProjectAsync(Guid id, CancellationToken ct);
    Task<PagedResult<TeamResponseDto>> TeamsAsync(Guid user, PageQueryDto query, CancellationToken ct);
    Task<TeamResponseDto?> TeamAsync(Guid id, Guid user, CancellationToken ct);
    Task<PagedResult<TeamMemberResponseDto>> TeamMembersAsync(Guid team, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<AttachmentResponseDto>> AttachmentsAsync(Guid task, Guid user, bool admin, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ActivityLogResponseDto>> TaskActivityAsync(Guid task, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ProjectActivityResponseDto>> ProjectActivityAsync(Guid project, PageQueryDto query, CancellationToken ct);
    Task<Board?> BoardAsync(Guid id, CancellationToken ct);
    Task<PagedResult<BoardSummaryDto>> BoardsAsync(Guid user, bool admin, Guid? project, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<CommentResponseDto>> CommentsAsync(Guid task, Guid user, PageQueryDto query, CancellationToken ct);
    Task<PagedResult<ProjectMemberDto>> MembersAsync(Guid project, PageQueryDto query, CancellationToken ct);
}
