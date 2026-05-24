using TaskHub.backend.DTOs;

namespace TaskHub.backend.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Register new user. Throws ConflictException if email exists.</summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);

    /// <summary>Login user. Throws UnauthorizedException if invalid credentials.</summary>
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);

    /// <summary>Login via Google OAuth. Throws UnauthorizedException on invalid token.</summary>
    Task<AuthResponseDto> GoogleLoginAsync(GoogleAuthDto dto, CancellationToken ct = default);

    /// <summary>Refresh access token. Throws UnauthorizedException if token invalid/expired.</summary>
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Logout — revoke refresh token.</summary>
    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default);

    /// <summary>Get current user profile. Throws NotFoundException if user not found.</summary>
    Task<UserResponseDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
