namespace TaskHub.Application.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public UserResponseDto User { get; set; } = null!;
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}



