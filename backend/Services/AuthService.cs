using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;
using TaskHub.backend.Exceptions;
using TaskHub.backend.Models;
using TaskHub.backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;

namespace TaskHub.backend.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, ITokenService tokenService, ILogger<AuthService> logger, IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email, ct))
            throw new ConflictException("Email already exists.");

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

        _logger.LogInformation("User {UserId} registered with email {Email}", user.Id, user.Email);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email {Email}", dto.Email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated account {UserId}", user.Id);
            throw new ForbiddenException("Account is deactivated. Contact an administrator.");
        }

        // Revoke all existing refresh tokens before issuing new ones
        await _tokenService.RevokeAllUserTokensAsync(user.Id, ct);

        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleAuthDto dto, CancellationToken ct = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(dto.Credential, settings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed Google token validation");
            throw new UnauthorizedException("Invalid Google token.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email, ct);

        if (user == null)
        {
            // Auto register if user doesn't exist
            user = new AppUser
            {
                Email = payload.Email,
                FullName = payload.Name ?? payload.Email,
                AvatarUrl = payload.Picture,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), 12), // Dummy password
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("New user {UserId} auto-registered via Google", user.Id);
        }
        else
        {
            if (!user.IsActive)
            {
                _logger.LogWarning("Google login attempt for deactivated account {UserId}", user.Id);
                throw new ForbiddenException("Account is deactivated.");
            }
            // Update avatar if missing
            if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
            {
                user.AvatarUrl = payload.Picture;
                await _context.SaveChangesAsync(ct);
            }
        }

        await _tokenService.RevokeAllUserTokensAsync(user.Id, ct);
        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        _logger.LogInformation("User {UserId} logged in via Google", user.Id);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshTokenStr, CancellationToken ct = default)
    {
        var existingToken = await _tokenService.ValidateRefreshTokenAsync(refreshTokenStr, ct);

        if (existingToken == null)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        // Token rotation: revoke old, issue new
        await _tokenService.RevokeRefreshTokenAsync(refreshTokenStr, ct);

        var user = existingToken.User;
        var newJwt = _tokenService.GenerateJwtToken(user);
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return new AuthResponseDto
        {
            Token = newJwt,
            RefreshToken = newRefreshToken.Token,
            User = MapToDto(user)
        };
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(refreshToken))
            await _tokenService.RevokeRefreshTokenAsync(refreshToken, ct);

        _logger.LogInformation("User {UserId} logged out", userId);
    }

    public async Task<UserResponseDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("User", userId);

        return MapToDto(user);
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
