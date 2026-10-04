using System.ComponentModel.DataAnnotations;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.DTOs;

// â”€â”€ Settings DTOs â”€â”€

public class UpdateProfileDto
{
    [Required, MinLength(2)]
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = null!;
}

// â”€â”€ Task DTOs â”€â”€

public class CreateTaskDto
{
    [Required]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TaskItemPriority? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public string? Label { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid? TeamId { get; set; }
}

public class UpdateTaskDto
{
    [Required]
    public string Title { get; set; } = null!;
    private string? _description;
    public string? Description { get => _description; set { _description = value; DescriptionSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool DescriptionSpecified { get; private set; }
    private TaskItemStatus _status = TaskItemStatus.Todo;
    public TaskItemStatus Status { get => _status; set { _status = value; StatusSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool StatusSpecified { get; private set; }
    private TaskItemPriority _priority = TaskItemPriority.Medium;
    public TaskItemPriority Priority { get => _priority; set { _priority = value; PrioritySpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool PrioritySpecified { get; private set; }
    private DateTime? _dueDate;
    public DateTime? DueDate { get => _dueDate; set { _dueDate = value; DueDateSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool DueDateSpecified { get; private set; }
    private DateTime? _startDate;
    public DateTime? StartDate { get => _startDate; set { _startDate = value; StartDateSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool StartDateSpecified { get; private set; }
    private string? _label;
    public string? Label { get => _label; set { _label = value; LabelSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool LabelSpecified { get; private set; }
    private int _progress;
    public int Progress { get => _progress; set { _progress = value; ProgressSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ProgressSpecified { get; private set; }
    private Guid? _assignedToId;
    public Guid? AssignedToId { get => _assignedToId; set { _assignedToId = value; AssignmentSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool AssignmentSpecified { get; private set; }
}

public class MoveTaskDto
{
    private DateTime? _expectedUpdatedAt;
    public DateTime? ExpectedUpdatedAt { get => _expectedUpdatedAt; set { _expectedUpdatedAt = value; ExpectedUpdatedAtSpecified = true; } }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ExpectedUpdatedAtSpecified { get; private set; }
    [Required]
    public Guid ListId { get; set; }
    public int Position { get; set; }
}

public class UpdateProgressDto
{
    [Range(0, 100)]
    public int Progress { get; set; }
}




