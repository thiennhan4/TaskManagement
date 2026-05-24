using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;
using TaskHub.backend.Exceptions;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public SettingsService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("User", userId);

        user.FullName = dto.FullName;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(userId, "UpdateProfile", "User", userId,
            new { dto.FullName, dto.AvatarUrl }, ct);

        return MapToDto(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("User", userId);

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new BusinessValidationException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens — force logout all devices
        var allTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var t in allTokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(userId, "ChangePassword", "User", userId, null, ct);
    }

    private static UserResponseDto MapToDto(Models.AppUser user) => new()
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
