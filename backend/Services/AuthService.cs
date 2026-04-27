using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;
using TaskHub.backend.Models;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email, ct))
            return ApiResponse<AuthResponseDto>.Fail("Email already exists.");

        var user = new AppUser
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            FullName = dto.FullName,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        }, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

        if (!user.IsActive)
            return ApiResponse<AuthResponseDto>.Fail("Account is deactivated. Contact an administrator.");

        // Revoke all existing refresh tokens before issuing new ones
        await _tokenService.RevokeAllUserTokensAsync(user.Id, ct);

        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        });
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshTokenStr, CancellationToken ct = default)
    {
        var existingToken = await _tokenService.ValidateRefreshTokenAsync(refreshTokenStr, ct);

        if (existingToken == null)
            return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");

        // Token rotation: revoke old, issue new
        await _tokenService.RevokeRefreshTokenAsync(refreshTokenStr, ct);

        var user = existingToken.User;
        var newJwt = _tokenService.GenerateJwtToken(user);
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = newJwt,
            RefreshToken = newRefreshToken.Token,
            User = MapToDto(user)
        });
    }

    public async Task<ApiResponse<object>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(refreshToken))
            await _tokenService.RevokeRefreshTokenAsync(refreshToken, ct);

        return ApiResponse<object>.Ok(null!, "Logged out successfully.");
    }

    public async Task<ApiResponse<UserResponseDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
            return ApiResponse<UserResponseDto>.Fail("User not found.");

        return ApiResponse<UserResponseDto>.Ok(MapToDto(user));
    }

    private static UserResponseDto MapToDto(AppUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        AvatarUrl = user.AvatarUrl,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
