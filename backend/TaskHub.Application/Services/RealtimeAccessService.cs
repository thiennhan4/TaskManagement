using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Exceptions;

namespace TaskHub.Application.Services;

public class RealtimeAccessService : IRealtimeAccessService
{
    private readonly IBoardRepository _boards;
    private readonly ITaskItemRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly ITeamRepository _teams;
    private readonly IPermissionService _permissions;
    private readonly IUserRepository _users;

    public RealtimeAccessService(IBoardRepository boards, ITaskItemRepository tasks,
        IProjectRepository projects, ITeamRepository teams, IUserRepository users, IPermissionService permissions)
    {
        _boards = boards;
        _tasks = tasks;
        _projects = projects;
        _teams = teams;
        _users = users;
        _permissions = permissions;
    }

    public async Task AuthorizeBoardAsync(Guid userId, Guid boardId, CancellationToken ct = default)
    {
        var board = await _boards.GetBoardByIdAsync(boardId, ct) ?? throw new NotFoundException("Board", boardId);
        await _permissions.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);
    }

    public async Task AuthorizeTaskAsync(Guid userId, Guid taskId, CancellationToken ct = default)
    {
        var task = await _tasks.GetTaskForAuthorizationAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        await _permissions.AuthorizeTaskActionAsync(userId, task, TaskAction.View, ct);
    }

    public async Task AuthorizeProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId, ct) ?? throw new NotFoundException("Project", projectId);
        await _permissions.AuthorizeProjectActionAsync(userId, project, ProjectAction.View, ct);
    }

    public async Task AuthorizeTeamAsync(Guid userId, Guid teamId, CancellationToken ct = default)
    {
        _ = await _teams.GetTeamByIdAsync(teamId, ct) ?? throw new NotFoundException("Team", teamId);
        await _permissions.AuthorizeTeamActionAsync(userId, teamId, TeamAction.View, ct);
    }

    public async Task<(string FullName, string AvatarUrl)> GetUserDisplayAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        return (user.FullName, user.AvatarUrl ?? "");
    }
}
