using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.controller;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ─── POST /api/auth/register ───
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(dto, ct);
        if (!result.Success) return BadRequest(result);

        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(result);
    }

    // ─── POST /api/auth/login ───
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        if (!result.Success) return Unauthorized(result);

        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(result);
    }

    // ─── POST /api/auth/refresh ───
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(ApiResponse<object>.Fail("No refresh token found."));

        var result = await _authService.RefreshTokenAsync(refreshToken, ct);
        if (!result.Success) return Unauthorized(result);

        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(result);
    }

    // ─── POST /api/auth/logout ───
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var refreshToken = Request.Cookies["refreshToken"];

        var result = await _authService.LogoutAsync(userId, refreshToken, ct);

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        return Ok(result);
    }

    // ─── GET /api/auth/me ───
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetCurrentUserAsync(userId, ct);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // ═══ PRIVATE HELPERS ═══

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/"
        });
    }
}
