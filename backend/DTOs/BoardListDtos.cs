using System.ComponentModel.DataAnnotations;

namespace TaskHub.backend.DTOs;

// ── BoardList Request DTOs ──

public class CreateBoardListDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = null!;
    public Guid BoardId { get; set; }
    public int Position { get; set; }
    public string? Color { get; set; }
}

public class UpdateBoardListDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = null!;
    public int Position { get; set; }
    public string? Color { get; set; }
}

// ── BoardList Response DTO ──

public class BoardListResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Position { get; set; }
    public Guid BoardId { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TaskResponseDto> Tasks { get; set; } = new();
}
