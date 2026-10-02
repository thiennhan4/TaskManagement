using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services;
using TaskHub.Domain.Exceptions;

namespace TaskHub.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool unreadOnly = false, CancellationToken ct = default)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedException("A valid user identity is required.");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize, unreadOnly, ct);
            return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(notifications, "Notifications loaded."));
        }

        [HttpGet("/api/v1/notifications")]
        public async Task<IActionResult> GetPage([FromQuery] PageQueryDto query, CancellationToken ct)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                throw new UnauthorizedException("A valid user identity is required.");
            return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(await _notificationService.GetPageAsync(userId, query, ct)));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedException("A valid user identity is required.");

            var count = await _notificationService.GetUnreadCountAsync(userId, ct);
            return Ok(ApiResponse<int>.Ok(count));
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct = default)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedException("A valid user identity is required.");

            await _notificationService.MarkAsReadAsync(id, userId, ct);
            return Ok(ApiResponse<object>.Ok(null!, "Notification marked as read."));
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedException("A valid user identity is required.");

            await _notificationService.MarkAllAsReadAsync(userId, ct);
            return Ok(ApiResponse<object>.Ok(null!, "Notifications marked as read."));
        }
    }
}



