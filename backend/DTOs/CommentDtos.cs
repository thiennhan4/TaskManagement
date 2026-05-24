using System.ComponentModel.DataAnnotations;

namespace TaskHub.backend.DTOs;

// ── Comment Request DTO ──

public class CreateCommentDto
{
    [Required, MinLength(1)]
    public string Content { get; set; } = null!;
}

// ── Comment Response DTO ──

public class CommentResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? UserAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsOwner { get; set; }
}
