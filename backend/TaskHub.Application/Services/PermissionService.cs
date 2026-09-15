using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;

namespace TaskHub.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;

    public PermissionService(
        IProjectRepository projectRepository,
        ITeamRepository teamRepository,
        IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _teamRepository = teamRepository;
        _userRepository = userRepository;
    }

    public async Task AuthorizeTaskActionAsync(Guid userId, TaskItem task, TaskAction action, CancellationToken ct = default)
    {
        if (await IsAdminAsync(userId, ct))
            return;

        var project = task.List?.Board?.Project;
        if (project != null)
        {
            if (action == TaskAction.View && (task.OwnerId == userId || task.AssignedToId == userId))
                return;

            if ((action == TaskAction.Update || action == TaskAction.ChangeStatus) &&
                (task.OwnerId == userId || task.AssignedToId == userId))
                return;

            await AuthorizeProjectTaskActionAsync(userId, project, action, ct);
            return;
        }

        if (task.TeamId.HasValue)
        {
            await AuthorizeLegacyTeamTaskActionAsync(userId, task, action, ct);
            return;
        }

        if (task.OwnerId == userId ||
            (action is TaskAction.View or TaskAction.Update or TaskAction.ChangeStatus && task.AssignedToId == userId))
            return;

        throw new ForbiddenException("You do not have access to this task.");
    }

    public async Task AuthorizeBoardActionAsync(Guid userId, Board board, BoardAction action, CancellationToken ct = default)
    {
        if (await IsAdminAsync(userId, ct))
            return;

        if (board.ProjectId.HasValue)
        {
            var project = board.Project ?? await _projectRepository.GetByIdAsync(board.ProjectId.Value)
                ?? throw new NotFoundException("Project", board.ProjectId.Value);

            var projectAction = action switch
            {
                BoardAction.View => ProjectAction.View,
                BoardAction.CreateTask => ProjectAction.CreateTask,
                _ => ProjectAction.Update
            };

            await AuthorizeProjectActionAsync(userId, project, projectAction, ct);
            return;
        }

        if (board.OwnerId == userId)
            return;

        throw new ForbiddenException("You do not have access to this board.");
    }

    public async Task AuthorizeProjectActionAsync(Guid userId, Project project, ProjectAction action, CancellationToken ct = default)
    {
        if (await IsAdminAsync(userId, ct))
            return;

        if (project.ProjectType == ProjectType.Personal)
        {
            if (project.OwnerId == userId)
                return;

            throw new ForbiddenException("You can only access your own personal projects.");
        }

        if (project.OwnerId == userId)
            return;

        var projectRole = await _projectRepository.GetProjectRoleAsync(project.Id, userId, ct);
        if (projectRole.HasValue && IsProjectRoleAllowed(projectRole.Value, action))
            return;

        if (project.WorkspaceId.HasValue)
        {
            var teamRole = await GetUserRoleInTeamAsync(userId, project.WorkspaceId.Value, ct);
            if (teamRole.HasValue && IsTeamRoleAllowed(teamRole.Value, action))
                return;
        }

        throw new ForbiddenException("You do not have permission to access this project.");
    }

    public async Task AuthorizeTeamActionAsync(Guid userId, Guid teamId, TeamAction action, CancellationToken ct = default)
    {
        if (await IsAdminAsync(userId, ct))
            return;

        var teamRole = await GetUserRoleInTeamAsync(userId, teamId, ct);

        if (teamRole == null)
            throw new ForbiddenException("You are not a member of this team.");

        switch (action)
        {
            case TeamAction.View:
                return;

            case TeamAction.Update:
            case TeamAction.ManageMembers:
                if (teamRole == TeamRole.Owner || teamRole == TeamRole.Manager)
                    return;
                throw new ForbiddenException("Only Owner or Manager can manage the team.");

            case TeamAction.Delete:
                if (teamRole == TeamRole.Owner)
                    return;
                throw new ForbiddenException("Only the team Owner can delete the team.");

            default:
                throw new ForbiddenException();
        }
    }

    public async Task<TeamRole?> GetUserRoleInTeamAsync(Guid userId, Guid teamId, CancellationToken ct = default)
    {
        var membership = await _teamRepository.GetTeamMemberAsync(teamId, userId, ct);
        return membership?.Role;
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default)
    {
        return await _userRepository.IsAdminAsync(userId, ct);
    }

    private async Task AuthorizeProjectTaskActionAsync(Guid userId, Project project, TaskAction action, CancellationToken ct)
    {
        var projectAction = action switch
        {
            TaskAction.View => ProjectAction.View,
            TaskAction.Create => ProjectAction.CreateTask,
            TaskAction.Update => ProjectAction.CreateTask,
            TaskAction.ChangeStatus => ProjectAction.CreateTask,
            TaskAction.Assign => ProjectAction.Update,
            TaskAction.Delete => ProjectAction.Update,
            _ => ProjectAction.View
        };

        if (action == TaskAction.View)
        {
            await AuthorizeProjectActionAsync(userId, project, projectAction, ct);
            return;
        }

        var projectRole = await _projectRepository.GetProjectRoleAsync(project.Id, userId, ct);
        if (projectRole == ProjectRole.Guest)
            throw new ForbiddenException("Guests cannot modify project tasks.");

        await AuthorizeProjectActionAsync(userId, project, projectAction, ct);
    }

    private async Task AuthorizeLegacyTeamTaskActionAsync(Guid userId, TaskItem task, TaskAction action, CancellationToken ct)
    {
        var teamRole = await GetUserRoleInTeamAsync(userId, task.TeamId!.Value, ct);

        if (teamRole == null)
            throw new ForbiddenException("You are not a member of this team.");

        switch (teamRole.Value)
        {
            case TeamRole.Owner:
            case TeamRole.Manager:
                return;

            case TeamRole.Member:
                if (action == TaskAction.View ||
                    ((action == TaskAction.Update || action == TaskAction.ChangeStatus) && task.AssignedToId == userId))
                    return;
                throw new ForbiddenException("Members can only update tasks assigned to them.");

            default:
                throw new ForbiddenException();
        }
    }

    private static bool IsProjectRoleAllowed(ProjectRole role, ProjectAction action)
    {
        return role switch
        {
            ProjectRole.Owner => true,
            ProjectRole.Admin => action is ProjectAction.View or ProjectAction.Update or ProjectAction.ManageMembers or ProjectAction.CreateTask,
            ProjectRole.Member => action is ProjectAction.View or ProjectAction.CreateTask,
            ProjectRole.Guest => action == ProjectAction.View,
            _ => false
        };
    }

    private static bool IsTeamRoleAllowed(TeamRole role, ProjectAction action)
    {
        return role switch
        {
            TeamRole.Owner or TeamRole.Manager => action is ProjectAction.View or ProjectAction.Update or ProjectAction.ManageMembers or ProjectAction.CreateTask,
            TeamRole.Member => action is ProjectAction.View or ProjectAction.CreateTask,
            _ => false
        };
    }
}
