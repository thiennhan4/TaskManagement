using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IMutationRunner _mutations;
    private readonly DurableDelivery _delivery;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IAuditService _auditService;

    public SettingsService(IUserRepository users, IRefreshTokenRepository tokens, IAuditService auditService, IMutationRunner mutations, DurableDelivery delivery)
    {
        _mutations = mutations;
        _delivery = delivery;
        _users = users;
        _tokens = tokens;
        _auditService = auditService;
    }

    public Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default) =>
        _mutations.RunAsync(() => UpdateProfileAsyncCore(userId, dto, ct), ct);

    private async Task<UserResponseDto> UpdateProfileAsyncCore(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        user.FullName = dto.FullName;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _users.SaveChangesAsync(ct);

        await _auditService.LogAsync(userId, "UpdateProfile", "User", userId,
            new { dto.FullName, dto.AvatarUrl }, ct);

        return MapToDto(user);
    }

    public Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default) =>
        _mutations.RunAsync(() => ChangePasswordAsyncCore(userId, dto, ct), ct);

    private async Task ChangePasswordAsyncCore(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new BusinessValidationException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens â€” force logout all devices
        await _tokens.RevokeAllAsync(userId, ct);
        await _users.SaveChangesAsync(ct);

        await _auditService.LogAsync(userId, "ChangePassword", "User", userId, null, ct);
    }

    private static UserResponseDto MapToDto(AppUser user) =>  new()
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





