using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Application.Validators;
using TaskHub.Domain.Exceptions;
using FluentValidation;

namespace TaskHub.Application.Services;

public class CollaborationReadService(ICollaborationReadRepository reads, IBoardReadRepository boards,
    IProjectRepository projects, ICommentRepository comments, IPermissionService permissions) : ICollaborationReadService
{
    public async Task<PagedResult<BoardSummaryDto>> BoardsAsync(Guid user, Guid? project, PageQueryDto query, CancellationToken ct)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query, ct);
        if (project.HasValue) await AuthorizeProject(user, project.Value, ct);
        return await reads.BoardsAsync(user, await permissions.IsAdminAsync(user, ct), project, query, ct);
    }
    public async Task<PagedResult<KanbanColumnDto>> ColumnsAsync(Guid user, Guid board, KanbanQueryDto query, CancellationToken ct)
    {
        await new KanbanQueryValidator().ValidateAndThrowAsync(query, ct);
        var resource = await reads.BoardAsync(board, ct) ?? throw new NotFoundException("Board", board);
        await permissions.AuthorizeBoardActionAsync(user, resource, BoardAction.View, ct);
        return await boards.GetColumnsAsync(board, query, ct);
    }
    public async Task<PagedResult<CommentResponseDto>> CommentsAsync(Guid user, Guid task, PageQueryDto query, CancellationToken ct)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query, ct);
        var resource = await comments.GetTaskForAuthorizationAsync(task, ct) ?? throw new NotFoundException("Task", task);
        await permissions.AuthorizeTaskActionAsync(user, resource, TaskAction.View, ct);
        return await reads.CommentsAsync(task, user, query, ct);
    }
    public async Task<PagedResult<ProjectMemberDto>> MembersAsync(Guid user, Guid project, PageQueryDto query, CancellationToken ct)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query, ct);
        await AuthorizeProject(user, project, ct);
        return await reads.MembersAsync(project, query, ct);
    }
    private async Task AuthorizeProject(Guid user, Guid id, CancellationToken ct)
    {
        var resource = await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Project", id);
        await permissions.AuthorizeProjectActionAsync(user, resource, ProjectAction.View, ct);
    }
}
