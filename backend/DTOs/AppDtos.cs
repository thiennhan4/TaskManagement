using System.ComponentModel.DataAnnotations;

namespace TaskHub.backend.DTOs;

// ── Settings DTOs ──

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

// ── Task DTOs ──

public class CreateTaskDto
{
    [Required]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Label { get; set; }
}

public class UpdateTaskDto
{
    [Required]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "Todo";
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Label { get; set; }
    public int Progress { get; set; }
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
