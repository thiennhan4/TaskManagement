using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.DTOs;
using TaskHub.backend.Models;

namespace TaskHub.backend.controller;

[Route("api/settings")]
[ApiController]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── PUT /api/settings/profile ───
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return NotFound(ApiResponse<object>.Fail("User not found."));

        user.FullName = dto.FullName;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        await LogAudit(userId, "UpdateProfile", "User", userId,
            new { dto.FullName, dto.AvatarUrl }, ct);

        return Ok(ApiResponse<object>.Ok(MapUser(user), "Profile updated."));
    }

    // ─── PUT /api/settings/password ───
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return NotFound(ApiResponse<object>.Fail("User not found."));

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(ApiResponse<object>.Fail("Current password is incorrect."));

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

        await LogAudit(userId, "ChangePassword", "User", userId, null, ct);

        return Ok(ApiResponse<object>.Ok(null!, "Password changed. All devices will be signed out."));
    }

    // ═══ PRIVATE HELPERS ═══

    private async Task LogAudit(Guid userId, string action, string entityType, Guid? entityId, object? detail, CancellationToken ct)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail != null ? JsonSerializer.Serialize(detail) : null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(ct);
    }

    private static object MapUser(AppUser user) => new
    {
        user.Id,
        user.FullName,
        user.Email,
        Role = user.Role.ToString(),
        user.AvatarUrl,
        user.IsActive,
        user.CreatedAt
    };
}
