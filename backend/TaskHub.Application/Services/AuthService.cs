using TaskHub.Application.Models;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace TaskHub.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IGoogleIdentityVerifier _google;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository users, ITokenService tokenService, ILogger<AuthService> logger, IConfiguration configuration, IGoogleIdentityVerifier google)
    {
        _users = users;
        _google = google;
        _tokenService = tokenService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AuthResult> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await _users.GetByEmailAsync(dto.Email, ct) is not null)
            throw new ConflictException("Email already exists.");

        var user = new AppUser
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            FullName = dto.FullName,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user, ct);

        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        _logger.LogInformation("User {UserId} registered with email {Email}", user.Id, user.Email);

        return new AuthResult
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResult> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(dto.Email, ct);

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

        return new AuthResult
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResult> GoogleLoginAsync(GoogleAuthDto dto, CancellationToken ct = default)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ServiceUnavailableException("Google login is not configured.");

        var payload = await _google.VerifyAsync(dto.Credential, clientId, ct);
        if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email) || !payload.EmailVerified)
            throw new UnauthorizedException("Google identity must have a verified email and subject.");
        var domain = payload.Email.Split('@').Last();
        var authoritative = domain.Equals("gmail.com", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(payload.HostedDomain) && domain.Equals(payload.HostedDomain, StringComparison.OrdinalIgnoreCase));
        if (!authoritative)
            throw new ForbiddenException("This Google account requires explicit account linking. Use email/password login.");
        var user = await _users.GetByEmailAsync(payload.Email, ct);

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
            await _users.AddAsync(user, ct);
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
                await _users.SaveChangesAsync(ct);
            }
        }

        await _tokenService.RevokeAllUserTokensAsync(user.Id, ct);
        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ct);

        _logger.LogInformation("User {UserId} logged in via Google", user.Id);

        return new AuthResult
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshTokenStr, CancellationToken ct = default)
    {
        var existingToken = await _tokenService.ValidateRefreshTokenAsync(refreshTokenStr, ct);

        if (existingToken == null)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        if (!existingToken.User.IsActive)
        {
            await _tokenService.RevokeAllUserTokensAsync(existingToken.UserId, ct);
            throw new ForbiddenException("Account is deactivated.");
        }

        // The repository conditionally consumes and inserts the successor in one transaction.


        var user = existingToken.User;
        var newJwt = _tokenService.GenerateJwtToken(user);
        var newRefreshToken = await _tokenService.RotateRefreshTokenAsync(refreshTokenStr, user.Id, ct)
            ?? throw new UnauthorizedException("Refresh token was already consumed or is no longer valid.");

        _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return new AuthResult
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
        var user = await _users.GetByIdAsync(userId, ct)
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





