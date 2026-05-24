using TaskHub.backend.Models;

namespace TaskHub.backend.DTOs;

/// <summary>
/// Standard response DTO for TaskItem — never expose Entity directly.
/// </summary>
public class TaskResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskItemPriority Priority { get; set; }
    public string? Label { get; set; }
    public int Position { get; set; }
    public int Progress { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public Guid ListId { get; set; }
    public string? ListName { get; set; }
    public Guid? BoardId { get; set; }
    public string? BoardName { get; set; }
    public Guid OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public Guid? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public Guid? TeamId { get; set; }
    // WorkspaceId = TeamId (direct) OR project's WorkspaceId — null means personal
    public Guid? WorkspaceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int CommentsCount { get; set; }
    public int AttachmentsCount { get; set; }
    public bool IsOverdue { get; set; }
}
