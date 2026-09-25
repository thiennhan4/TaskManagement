using Microsoft.AspNetCore.RateLimiting;
﻿using System.Security.Claims;
using TaskHub.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.controller;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // â”€â”€â”€ POST /api/auth/register â”€â”€â”€
    [HttpPost("register")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(dto, ct);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result.ToResponse(), "Registration successful."));
    }

    // â”€â”€â”€ POST /api/auth/login â”€â”€â”€
    [HttpPost("login")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result.ToResponse(), "Login successful."));
    }

    [HttpPost("google")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto dto, CancellationToken ct)
    {
        var result = await _authService.GoogleLoginAsync(dto, ct);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result.ToResponse(), "Google login successful."));
    }

    // â”€â”€â”€ POST /api/auth/refresh â”€â”€â”€
    [HttpPost("refresh")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedException("No refresh token found.");

        var result = await _authService.RefreshTokenAsync(refreshToken, ct);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result.ToResponse()));
    }

    // â”€â”€â”€ POST /api/auth/logout â”€â”€â”€
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var refreshToken = Request.Cookies["refreshToken"];

        await _authService.LogoutAsync(userId, refreshToken, ct);

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        return Ok(ApiResponse<object>.Ok(null!, "Logged out successfully."));
    }

    // â”€â”€â”€ GET /api/auth/me â”€â”€â”€
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var user = await _authService.GetCurrentUserAsync(userId, ct);
        return Ok(ApiResponse<UserResponseDto>.Ok(user));
    }

    // â•â•â• PRIVATE HELPERS â•â•â•

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



