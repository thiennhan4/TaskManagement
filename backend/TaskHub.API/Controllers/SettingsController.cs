using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

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

    // â”€â”€â”€ PUT /api/settings/profile â”€â”€â”€
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var user = await _settingsService.UpdateProfileAsync(GetCurrentUserId(), dto, ct);
        return Ok(ApiResponse<UserResponseDto>.Ok(user, "Profile updated."));
    }

    // â”€â”€â”€ PUT /api/settings/password â”€â”€â”€
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        await _settingsService.ChangePasswordAsync(GetCurrentUserId(), dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Password changed. All devices will be signed out."));
    }
}



