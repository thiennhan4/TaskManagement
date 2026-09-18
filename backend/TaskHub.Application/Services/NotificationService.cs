using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskHub.Application.Data;
using TaskHub.Application.DTOs;
using TaskHub.Application.Hubs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services;

public class NotificationService : INotificationService
{
    private const string GeneralTypeCode = "GENERAL";
    private readonly IAppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IAppDbContext context,
        IHubContext<NotificationHub> hubContext,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _emailService = emailService;
        _logger = logger;
    }

    public Task CreateNotificationAsync(Guid userId, string title, string message, string? linkUrl = null)
    {
        return SendAsync(new NotificationRequest
        {
            UserId = userId,
            TypeCode = GeneralTypeCode,
            Channels = new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Email },
            Title = title,
            Message = message,
            ActionUrl = linkUrl
        });
    }

    public async Task<NotificationResult> SendAsync(NotificationRequest request)
    {
        if (request.UserId == Guid.Empty)
        {
            return NotificationResult.Fail("UserId is required.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
        if (user == null)
        {
            return NotificationResult.Fail("Recipient user was not found.");
        }

        var type = await GetOrCreateNotificationTypeAsync(request.TypeCode);
        var channels = await ResolveChannelsAsync(request, type.Id);
        if (channels.Count == 0)
        {
            return NotificationResult.Fail("No notification channels are enabled for this user.");
        }

        var data = request.Data ?? new Dictionary<string, object>();
        var inAppTemplate = await GetTemplateAsync(type.Id, NotificationChannel.InApp);
        var title = Render(request.Title ?? inAppTemplate?.TitleTemplate ?? type.Name, data);
        var message = Render(request.Message ?? inAppTemplate?.MessageTemplate ?? string.Empty, data);

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            return NotificationResult.Fail("Notification title and message are required.");
        }

        var notification = new Notification
        {
            UserId = request.UserId,
            NotificationTypeId = type.Id,
            Title = title,
            Message = message,
            Data = data.Count > 0 ? JsonSerializer.Serialize(data) : null,
            ActionUrl = request.ActionUrl,
            ActionLabel = request.ActionLabel,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var dto = ToDto(notification, type.Code);
        foreach (var channel in channels)
        {
            await DeliverAsync(notification, dto, user, type.Id, channel, data);
        }

        return NotificationResult.Ok(notification.Id);
    }

    public async Task<BatchNotificationResult> SendBatchAsync(BatchNotificationRequest request)
    {
        var result = new BatchNotificationResult
        {
            Total = request.Notifications.Count
        };

        foreach (var notification in request.Notifications)
        {
            var itemResult = await SendAsync(notification);
            result.Results.Add(itemResult);
            if (itemResult.Success)
            {
                result.Sent++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Notifications
            .AsNoTracking()
            .Include(n => n.NotificationType)
            .Where(n => n.UserId == userId && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow));

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => ToDto(n, n.NotificationType.Code))
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(userId.ToString()).SendAsync("NotificationRead", notificationId);
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group(userId.ToString()).SendAsync("AllNotificationsRead");
    }

    public Task<int> GetUnreadCountAsync(Guid userId)
    {
        return _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow));
    }

    private async Task DeliverAsync(
        Notification notification,
        NotificationDto dto,
        AppUser user,
        int notificationTypeId,
        NotificationChannel channel,
        Dictionary<string, object> data)
    {
        var delivery = new NotificationDelivery
        {
            NotificationId = notification.Id,
            Channel = channel.ToString(),
            RecipientEmail = channel == NotificationChannel.Email ? user.Email : null,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.NotificationDeliveries.Add(delivery);
        await _context.SaveChangesAsync();

        try
        {
            delivery.AttemptCount++;
            delivery.LastAttemptAt = DateTime.UtcNow;

            if (channel == NotificationChannel.InApp)
            {
                await _hubContext.Clients.Group(user.Id.ToString()).SendAsync("ReceiveNotification", dto);
                delivery.Status = "Sent";
                delivery.SentAt = DateTime.UtcNow;
            }
            else if (channel == NotificationChannel.Email && !string.IsNullOrWhiteSpace(user.Email))
            {
                var template = await GetTemplateAsync(notificationTypeId, NotificationChannel.Email);
                var subject = Render(template?.Subject ?? $"TaskHub: {notification.Title}", data);
                var body = Render(template?.BodyTemplate ?? BuildDefaultEmailBody(notification), data);

                await _emailService.SendEmailAsync(user.Email, subject, body, true);
                delivery.Status = "Sent";
                delivery.SentAt = DateTime.UtcNow;
                delivery.ProviderName = "SMTP";
            }
            else
            {
                delivery.Status = "Failed";
                delivery.StatusMessage = $"{channel} delivery is not configured.";
            }
        }
        catch (Exception ex)
        {
            delivery.Status = "Failed";
            delivery.StatusMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(15);
            _logger.LogError(ex, "Failed to deliver notification {NotificationId} through {Channel}", notification.Id, channel);
        }

        await _context.SaveChangesAsync();
    }

    private async Task<NotificationType> GetOrCreateNotificationTypeAsync(string? typeCode)
    {
        var code = string.IsNullOrWhiteSpace(typeCode) ? GeneralTypeCode : typeCode.Trim().ToUpperInvariant();
        var type = await _context.NotificationTypes.FirstOrDefaultAsync(t => t.Code == code);
        if (type != null)
        {
            return type;
        }

        type = new NotificationType
        {
            Code = code,
            Name = code == GeneralTypeCode ? "General" : code.Replace("_", " "),
            DefaultChannels = "InApp,Email",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.NotificationTypes.Add(type);
        await _context.SaveChangesAsync();
        return type;
    }

    private async Task<List<NotificationChannel>> ResolveChannelsAsync(NotificationRequest request, int notificationTypeId)
    {
        var channels = request.Channels?.Count > 0
            ? request.Channels.Distinct().ToList()
            : await GetDefaultChannelsAsync(notificationTypeId);

        var preference = await _context.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.NotificationTypeId == notificationTypeId);

        if (preference == null)
        {
            return channels;
        }

        return channels.Where(channel => channel switch
        {
            NotificationChannel.Email => preference.AllowEmail,
            NotificationChannel.InApp => preference.AllowInApp,
            NotificationChannel.SMS => preference.AllowSMS,
            NotificationChannel.Push => preference.AllowPush,
            _ => false
        }).ToList();
    }

    private async Task<List<NotificationChannel>> GetDefaultChannelsAsync(int notificationTypeId)
    {
        var defaultChannels = await _context.NotificationTypes
            .Where(t => t.Id == notificationTypeId)
            .Select(t => t.DefaultChannels)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(defaultChannels))
        {
            return new List<NotificationChannel> { NotificationChannel.InApp };
        }

        return defaultChannels
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Enum.TryParse<NotificationChannel>(value, true, out var channel) ? channel : (NotificationChannel?)null)
            .Where(channel => channel.HasValue)
            .Select(channel => channel!.Value)
            .Distinct()
            .ToList();
    }

    private Task<NotificationTemplate?> GetTemplateAsync(int notificationTypeId, NotificationChannel channel)
    {
        return _context.NotificationTemplates
            .AsNoTracking()
            .Where(t => t.NotificationTypeId == notificationTypeId && t.Channel == channel.ToString() && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync();
    }

    private static string Render(string template, Dictionary<string, object> data)
    {
        var rendered = template;
        foreach (var (key, value) in data)
        {
            var text = value?.ToString() ?? string.Empty;
            rendered = rendered
                .Replace("{{" + key + "}}", text, StringComparison.OrdinalIgnoreCase)
                .Replace("{" + key + "}", text, StringComparison.OrdinalIgnoreCase);
        }

        return rendered;
    }

    private static string BuildDefaultEmailBody(Notification notification)
    {
        var body = $"<h3>{notification.Title}</h3><p>{notification.Message}</p>";
        if (!string.IsNullOrWhiteSpace(notification.ActionUrl))
        {
            body += $"<p><a href=\"http://localhost:5173{notification.ActionUrl}\">{notification.ActionLabel ?? "Open in TaskHub"}</a></p>";
        }

        return body;
    }

    private static NotificationDto ToDto(Notification notification, string typeCode)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = typeCode,
            ActionUrl = notification.ActionUrl,
            LinkUrl = notification.ActionUrl,
            ActionLabel = notification.ActionLabel,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt,
            ExpiresAt = notification.ExpiresAt,
            Data = DeserializeData(notification.Data)
        };
    }

    private static Dictionary<string, object>? DeserializeData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }
}





