using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services.Interfaces;

/// <summary>
/// Resource-based authorization â€” core permission logic per backendplan Â§5.4.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if user can perform the given action on a task.
    /// Throws ForbiddenException if denied.
    /// </summary>
    Task AuthorizeTaskActionAsync(Guid userId, TaskItem task, TaskAction action, CancellationToken ct = default);

    /// <summary>
    /// Check if user can perform the given action on a team.
    /// Throws ForbiddenException if denied.
    /// </summary>
    Task AuthorizeTeamActionAsync(Guid userId, Guid teamId, TeamAction action, CancellationToken ct = default);

    /// <summary>
    /// Check if user can perform the given action on a project.
    /// - Personal project: only OwnerId == userId is allowed.
    /// - Team project: delegates to team RBAC.
    /// Throws ForbiddenException if denied.
    /// </summary>
    Task AuthorizeProjectActionAsync(Guid userId, Project project, ProjectAction action, CancellationToken ct = default);

    /// <summary>
    /// Get user's role in a specific team. Returns null if not a member.
    /// </summary>
    Task<TeamRole?> GetUserRoleInTeamAsync(Guid userId, Guid teamId, CancellationToken ct = default);

    /// <summary>
    /// Check if user is a global Admin.
    /// </summary>
    Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Actions that can be performed on a TaskItem.
/// </summary>
public enum TaskAction
{
    View,
    Create,
    Update,
    Delete,
    Assign
}

/// <summary>
/// Actions that can be performed on a Team.
/// </summary>
public enum TeamAction
{
    View,
    Update,
    Delete,
    ManageMembers
}

/// <summary>
/// Actions that can be performed on a Project.
/// </summary>
public enum ProjectAction
{
    View,
    Update,
    Delete,
    ManageMembers,
    CreateTask
}





