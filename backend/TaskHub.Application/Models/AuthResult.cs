using TaskHub.Application.DTOs;

namespace TaskHub.Application.Models;

// Internal result: only the controller may move the refresh secret to a cookie.
public sealed class AuthResult
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public UserResponseDto User { get; set; } = null!;
    public AuthResponseDto ToResponse() => new() { Token = Token, User = User };
}
