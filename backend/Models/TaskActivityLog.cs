namespace TaskHub.backend.Models;

public enum ActivityLogAction
{
    Created,
    Updated,
    StatusChanged,
    Assigned,
    Unassigned,
    Commented,
    CommentDeleted,
    AttachmentAdded,
    AttachmentRemoved
}

public class TaskActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public ActivityLogAction Action { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
