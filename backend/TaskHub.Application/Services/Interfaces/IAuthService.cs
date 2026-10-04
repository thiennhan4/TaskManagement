using TaskHub.Application.Models;
﻿using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Register new user. Throws ConflictException if email exists.</summary>
    Task<AuthResult> RegisterAsync(RegisterDto dto, CancellationToken ct = default);

    /// <summary>Login user. Throws UnauthorizedException if invalid credentials.</summary>
    Task<AuthResult> LoginAsync(LoginDto dto, CancellationToken ct = default);

    /// <summary>Login via Google OAuth. Throws UnauthorizedException on invalid token.</summary>
    Task<AuthResult> GoogleLoginAsync(GoogleAuthDto dto, CancellationToken ct = default);

    /// <summary>Refresh access token. Throws UnauthorizedException if token invalid/expired.</summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Logout â€” revoke refresh token.</summary>
    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default);

    /// <summary>Get current user profile. Throws NotFoundException if user not found.</summary>
    Task<UserResponseDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}





