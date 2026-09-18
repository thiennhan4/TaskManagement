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
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public string? Label { get; set; }
    public int Progress { get; set; }
    public Guid? AssignedToId { get; set; }
}

public class MoveTaskDto
{
    [Required]
    public Guid ListId { get; set; }
    public int Position { get; set; }
}

public class UpdateProgressDto
{
    [Range(0, 100)]
    public int Progress { get; set; }
}




