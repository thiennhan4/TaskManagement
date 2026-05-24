namespace TaskHub.backend.Models;

/// <summary>
/// Task status values — stored as string in DB via HasConversion.
/// </summary>
public enum TaskItemStatus
{
    Todo,
    InProgress,
    Review,
    Done
}

/// <summary>
/// Task priority levels — stored as string in DB via HasConversion.
/// </summary>
public enum TaskItemPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Role of a user within a Team.
/// </summary>
public enum TeamRole
{
    Owner,
    Manager,
    Member
}

/// <summary>
/// Status of a Project.
/// </summary>
public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}

/// <summary>
/// Visibility/privacy of a Project.
/// </summary>
public enum ProjectVisibility
{
    Private,
    TeamOnly,
    Public
}

/// <summary>
/// Role of a user within a Project.
/// </summary>
public enum ProjectRole
{
    Owner,
    Admin,
    Member,
    Guest
}

/// <summary>
/// Action types for project activity log.
/// </summary>
public enum ProjectActivityAction
{
    Created,
    Updated,
    StatusChanged,
    VisibilityChanged,
    Archived,
    Restored,
    MemberInvited,
    MemberJoined,
    MemberRemoved,
    MemberRoleChanged,
    OwnershipTransferred
}

/// <summary>
/// Repeat types for recurring tasks.
/// </summary>
public enum RepeatType
{
    None,
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Custom
}
