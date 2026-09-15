using System.ComponentModel.DataAnnotations;

namespace TaskHub.Application.DTOs;

// â”€â”€ Comment Request DTO â”€â”€

public class CreateCommentDto
{
    [Required, MinLength(1)]
    public string Content { get; set; } = null!;
}

// â”€â”€ Comment Response DTO â”€â”€

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



