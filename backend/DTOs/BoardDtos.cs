using System.ComponentModel.DataAnnotations;

namespace TaskHub.backend.DTOs;

// ── Board Request DTOs ──

public class CreateBoardDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
}

public class UpdateBoardDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
}

// ── Board Response DTO ──

public class BoardResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BoardListResponseDto> Lists { get; set; } = new();
}
