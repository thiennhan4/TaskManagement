using TaskHub.backend.DTOs;

namespace TaskHub.backend.Services.Interfaces;

/// <summary>
/// User settings (profile, password) service.
/// </summary>
public interface ISettingsService
{
    Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);
}
