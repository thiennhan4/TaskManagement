using System.ComponentModel.DataAnnotations.Schema;

namespace TaskHub.Domain.Entities;

[Table("Tasks")]
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid ListId { get; set; }
    public Guid OwnerId { get; set; }
    public int Position { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public string? Label { get; set; }
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // â”€â”€ Calendar Features â”€â”€
    public DateTime? EndTime { get; set; }
    public bool IsAllDay { get; set; } = false;
    public string? Color { get; set; }
    public RepeatType RepeatType { get; set; } = RepeatType.None;
    public int? RepeatInterval { get; set; }
    public DateTime? RepeatEndDate { get; set; }
    public int? ReminderMinutesBefore { get; set; }
    public string? Location { get; set; }


    // â”€â”€ Phase 2: Team & Assignment â”€â”€
    public Guid? TeamId { get; set; }
    public Guid? AssignedToId { get; set; }

    // Navigation properties
    public BoardList List { get; set; } = null!;
    public AppUser Owner { get; set; } = null!;
    public Team? Team { get; set; }
    public AppUser? AssignedTo { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    public ICollection<TaskActivityLog> ActivityLogs { get; set; } = new List<TaskActivityLog>();
}



