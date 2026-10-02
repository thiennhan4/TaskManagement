using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Application.Validators;
using TaskHub.Domain.Exceptions;
using FluentValidation;

namespace TaskHub.Application.Services;

public partial class CollaborationReadService(ICollaborationReadRepository reads, IBoardReadRepository boards,
    IProjectRepository projects, ICommentRepository comments, IPermissionService permissions) : ICollaborationReadService
{
    public async Task<PagedResult<ProjectResponseDto>> ProjectsAsync(Guid user, ProjectQueryDto query, CancellationToken ct)
    {
        await new ProjectQueryValidator().ValidateAndThrowAsync(query,ct);
        if(query.WorkspaceId.HasValue) await permissions.AuthorizeTeamActionAsync(user,query.WorkspaceId.Value,TeamAction.View,ct);
        return await reads.ProjectsAsync(user,await permissions.IsAdminAsync(user,ct),query,ct);
    }
    public async Task<ProjectResponseDto> ProjectAsync(Guid user, Guid id, CancellationToken ct)
    {
        await AuthorizeProject(user,id,ct);
        var result = await reads.ProjectAsync(id,ct) ?? throw new NotFoundException("Project",id);
        result.CurrentUserRole = await projects.GetProjectRoleAsync(id,user,ct);
        return result;
    }
    public async Task<PagedResult<TeamResponseDto>> TeamsAsync(Guid user, PageQueryDto query, CancellationToken ct)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query,ct);
        return await reads.TeamsAsync(user,query,ct);
    }
    public async Task<TeamResponseDto> TeamAsync(Guid user, Guid id, CancellationToken ct)
    {
        await permissions.AuthorizeTeamActionAsync(user,id,TeamAction.View,ct);
        return await reads.TeamAsync(id,user,ct) ?? throw new NotFoundException("Team",id);
    }
    public async Task<PagedResult<TeamMemberResponseDto>> TeamMembersAsync(Guid user, Guid team, PageQueryDto query, CancellationToken ct)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query,ct);
        await permissions.AuthorizeTeamActionAsync(user,team,TeamAction.View,ct);
        return await reads.TeamMembersAsync(team,query,ct);
    }
    private async Task AuthorizeTask(Guid user, Guid task, PageQueryDto query, CancellationToken ct)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query,ct);
        var resource=await comments.GetTaskForAuthorizationAsync(task,ct) ?? throw new NotFoundException("Task",task);
        await permissions.AuthorizeTaskActionAsync(user,resource,TaskAction.View,ct);
    }
    public async Task<PagedResult<AttachmentResponseDto>> AttachmentsAsync(Guid user, Guid task, PageQueryDto query, CancellationToken ct)
    { await AuthorizeTask(user,task,query,ct); return await reads.AttachmentsAsync(task,user,await permissions.IsAdminAsync(user,ct),query,ct); }
    public async Task<PagedResult<ActivityLogResponseDto>> TaskActivityAsync(Guid user, Guid task, PageQueryDto query, CancellationToken ct)
    { await AuthorizeTask(user,task,query,ct); return await reads.TaskActivityAsync(task,query,ct); }
    public async Task<PagedResult<ProjectActivityResponseDto>> ProjectActivityAsync(Guid user, Guid project, PageQueryDto query, CancellationToken ct)
    { await new PageQueryValidator().ValidateAndThrowAsync(query,ct); await AuthorizeProject(user,project,ct); return await reads.ProjectActivityAsync(project,query,ct); }
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
