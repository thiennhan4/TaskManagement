using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.Exceptions;
using TaskHub.backend.Models;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

/// <summary>
/// Core authorization engine — implements backendplan §5.4 Resource-based Authorization.
///
/// Logic:
///   IF Admin → allow
///   IF Task.TeamId == NULL → only creator (OwnerId) can access
///   IF Task.TeamId != NULL → check team role:
///     Owner  → full access
///     Manager → full access
///     Member → view all, update only assigned tasks
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;

    public PermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AuthorizeTaskActionAsync(Guid userId, TaskItem task, TaskAction action, CancellationToken ct = default)
    {
        // Rule 1: Admin → always allow
        if (await IsAdminAsync(userId, ct))
            return;

        // Rule 2: Personal task (no team)
        if (task.TeamId == null)
        {
            if (task.OwnerId == userId)
                return;

            throw new ForbiddenException("You can only access your own personal tasks.");
        }

        // Rule 3: Team task — check role in team
        var teamRole = await GetUserRoleInTeamAsync(userId, task.TeamId.Value, ct);

        if (teamRole == null)
            throw new ForbiddenException("You are not a member of this team.");

        switch (teamRole.Value)
        {
            case TeamRole.Owner:
            case TeamRole.Manager:
                // Owner & Manager → full access to all team tasks
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

    public async Task AuthorizeTeamActionAsync(Guid userId, Guid teamId, TeamAction action, CancellationToken ct = default)
    {
        // Admin → always allow
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
