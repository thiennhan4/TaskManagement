using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Controllers;

[Route("api/settings")]
[ApiController]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── PUT /api/settings/profile ───
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var user = await _settingsService.UpdateProfileAsync(GetCurrentUserId(), dto, ct);
        return Ok(ApiResponse<UserResponseDto>.Ok(user, "Profile updated."));
    }

    // ─── PUT /api/settings/password ───
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        await _settingsService.ChangePasswordAsync(GetCurrentUserId(), dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Password changed. All devices will be signed out."));
    }
}
