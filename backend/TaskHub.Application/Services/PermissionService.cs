using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Data;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

/// <summary>
/// Core authorization engine â€” implements backendplan Â§5.4 Resource-based Authorization.
///
/// Logic:
///   IF Admin â†’ allow
///   IF Task.TeamId == NULL â†’ only creator (OwnerId) can access
///   IF Task.TeamId != NULL â†’ check team role:
///     Owner  â†’ full access
///     Manager â†’ full access
///     Member â†’ view all, update only assigned tasks
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IAppDbContext _context;

    public PermissionService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task AuthorizeTaskActionAsync(Guid userId, TaskItem task, TaskAction action, CancellationToken ct = default)
    {
        // Rule 1: Admin â†’ always allow
        if (await IsAdminAsync(userId, ct))
            return;

        // Rule 2: Personal task (no team)
        if (task.TeamId == null)
        {
            if (task.OwnerId == userId)
                return;

            throw new ForbiddenException("You can only access your own personal tasks.");
        }

        // Rule 3: Team task â€” check role in team
        var teamRole = await GetUserRoleInTeamAsync(userId, task.TeamId.Value, ct);

        if (teamRole == null)
            throw new ForbiddenException("You are not a member of this team.");

        switch (teamRole.Value)
        {
            case TeamRole.Owner:
            case TeamRole.Manager:
                // Owner & Manager â†’ full access to all team tasks
                return;

            case TeamRole.Member:
                switch (action)
                {
                    case TaskAction.View:
                        // Member can view all team tasks
                        return;

                    case TaskAction.Update:
                        // Member can only update tasks assigned to them
                        if (task.AssignedToId == userId)
                            return;
                        throw new ForbiddenException("Members can only update tasks assigned to them.");

                    case TaskAction.Create:
                    case TaskAction.Delete:
                    case TaskAction.Assign:
                        throw new ForbiddenException("Members cannot create, delete, or assign tasks.");

                    default:
                        throw new ForbiddenException();
                }

            default:
                throw new ForbiddenException();
        }
    }

    public async Task AuthorizeProjectActionAsync(Guid userId, Project project, ProjectAction action, CancellationToken ct = default)
    {
        // Admin → always allow
        if (await IsAdminAsync(userId, ct))
            return;

        // Personal project: only owner can access
        if (project.ProjectType == ProjectType.Personal)
        {
            if (project.OwnerId == userId)
                return;

            throw new ForbiddenException("You can only access your own personal projects.");
        }

        // Team project: delegate to team RBAC
        if (!project.WorkspaceId.HasValue)
            throw new ForbiddenException("Team project has no associated workspace.");

        var teamRole = await GetUserRoleInTeamAsync(userId, project.WorkspaceId.Value, ct);

        if (teamRole == null)
        {
            // Check if user is a direct ProjectMember (invited without team)
            var isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == project.Id && pm.UserId == userId, ct);
            if (!isMember)
                throw new ForbiddenException("You are not a member of this project.");
            return; // ProjectMember can view
        }

        switch (action)
        {
            case ProjectAction.View:
                // Any team member can view
                return;

            case ProjectAction.CreateTask:
                // Members and above can create tasks
                return;

            case ProjectAction.Update:
            case ProjectAction.ManageMembers:
                if (teamRole == TeamRole.Owner || teamRole == TeamRole.Manager)
                    return;
                throw new ForbiddenException("Only Owner or Manager can modify the project.");

            case ProjectAction.Delete:
                if (teamRole == TeamRole.Owner)
                    return;
                throw new ForbiddenException("Only the team Owner can delete the project.");

            default:
                throw new ForbiddenException();
        }
    }

    public async Task AuthorizeTeamActionAsync(Guid userId, Guid teamId, TeamAction action, CancellationToken ct = default)
    {
        // Admin â†’ always allow
        if (await IsAdminAsync(userId, ct))
            return;

        var teamRole = await GetUserRoleInTeamAsync(userId, teamId, ct);

        if (teamRole == null)
            throw new ForbiddenException("You are not a member of this team.");

        switch (action)
        {
            case TeamAction.View:
                // Any member can view
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
        var membership = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId, ct);

        return membership?.Role;
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        return user?.Role == UserRole.Admin;
    }
}





