using System.Text.Json;
using FluentValidation;
using TaskHub.Application.Validators;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.DTOs;
using TaskHub.Application.Hubs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services;

public class NotificationService : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetPageAsync(Guid userId, PageQueryDto query, CancellationToken ct = default)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query, ct);
        var total = await _repository.GetCountAsync(userId, ct);
        var items = await _repository.GetPageAsync(userId, query.Page, query.PageSize, false, ct);
        return new PagedResult<NotificationDto>
        {
            Items = items.Select(n => ToDto(n, n.NotificationType.Code)).ToList(),
            Page = query.Page, PageSize = query.PageSize, TotalItems = total
        };
    }
    private readonly IMutationRunner _mutations;
    private readonly DurableDelivery _delivery;
    private const string GeneralTypeCode = "GENERAL";
    private readonly INotificationRepository _repository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository context,
        IHubContext<NotificationHub> hubContext,
        IEmailService emailService,
        ILogger<NotificationService> logger, IMutationRunner mutations, DurableDelivery delivery)
    {
        _mutations = mutations;
        _delivery = delivery;
        _repository = context;
        _hubContext = hubContext;
        _emailService = emailService;
        _logger = logger;
    }

    public Task CreateNotificationAsync(Guid userId, string title, string message, string? linkUrl = null, CancellationToken ct = default)
    {
        return SendAsync(new NotificationRequest
        {
            UserId = userId,
            TypeCode = GeneralTypeCode,
            Channels = new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Email },
            Title = title,
            Message = message,
            ActionUrl = linkUrl
        }, ct);
    }

    public Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken ct = default) =>
        _mutations.RunAsync(() => SendAsyncCore(request, ct), ct);

    private async Task<NotificationResult> SendAsyncCore(NotificationRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return NotificationResult.Fail("UserId is required.");
        }

        var user = await _repository.GetRecipientAsync(request.UserId, ct);
        if (user == null)
        {
            return NotificationResult.Fail("Recipient user was not found.");
        }

        var type = await GetOrCreateNotificationTypeAsync(request.TypeCode, ct);
        var channels = await ResolveChannelsAsync(request, type.Id, ct);
        if (channels.Count == 0)
        {
            return NotificationResult.Fail("No notification channels are enabled for this user.");
        }

        var data = request.Data ?? new Dictionary<string, object>();
        var inAppTemplate = await GetTemplateAsync(type.Id, NotificationChannel.InApp, ct);
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

        await _repository.PersistAsync(notification, ct);

        var dto = ToDto(notification, type.Code);
        foreach (var channel in channels)
        {
            await DeliverAsync(notification, dto, user, type.Id, channel, data, ct);
        }

        return NotificationResult.Ok(notification.Id);
    }

    public async Task<BatchNotificationResult> SendBatchAsync(BatchNotificationRequest request, CancellationToken ct = default)
    {
        var result = new BatchNotificationResult
        {
            Total = request.Notifications.Count
        };

        foreach (var notification in request.Notifications)
        {
            var itemResult = await SendAsync(notification, ct);
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
        bool unreadOnly = false, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return (await _repository.GetPageAsync(userId, page, pageSize, unreadOnly, ct)).Select(n => ToDto(n, n.NotificationType.Code));
    }

    public Task MarkAsReadAsync(int notificationId, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => MarkAsReadAsyncCore(notificationId, userId, ct), ct);

    private async Task MarkAsReadAsyncCore(int notificationId, Guid userId, CancellationToken ct = default)
    {
        if (await _repository.MarkReadAsync(userId, notificationId, ct))
            await _delivery.EnqueueAsync("Realtime", new { Group = userId.ToString(), Event = "NotificationRead", Arguments = new object?[] {  notificationId } }, () => _hubContext.Clients.Group(userId.ToString()).SendAsync("NotificationRead", notificationId), ct);
    }
    public Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => MarkAllAsReadAsyncCore(userId, ct), ct);

    private async Task MarkAllAsReadAsyncCore(Guid userId, CancellationToken ct = default)
    {
        if (await _repository.MarkReadAsync(userId, null, ct))
            await _delivery.EnqueueAsync("Realtime", new { Group = userId.ToString(), Event = "AllNotificationsRead", Arguments = new object?[] {  } }, () => _hubContext.Clients.Group(userId.ToString()).SendAsync("AllNotificationsRead"), ct);
    }
    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) => _repository.GetUnreadCountAsync(userId, ct);

    private async Task DeliverAsync(
        Notification notification,
        NotificationDto dto,
        AppUser user,
        int notificationTypeId,
        NotificationChannel channel,
        Dictionary<string, object> data, CancellationToken ct = default)
    {
        var delivery = new NotificationDelivery
        {
            NotificationId = notification.Id,
            Channel = channel.ToString(),
            RecipientEmail = channel == NotificationChannel.Email ? user.Email : null,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddDeliveryAsync(delivery, ct);
        await _delivery.EnqueueAsync("NotificationDelivery", new { DeliveryId = delivery.Id, NotificationId = notification.Id, channel }, async () =>
        {

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
                var template = await GetTemplateAsync(notificationTypeId, NotificationChannel.Email, ct);
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
        catch (Exception)
        {
            delivery.Status = "Failed";
            delivery.StatusMessage = "Delivery failed; retry required.";
            delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(15);
            _logger.LogWarning("Failed to deliver notification {NotificationId} through {Channel}", notification.Id, channel);
        }

        await _repository.SaveDeliveryAsync(delivery, CancellationToken.None);
        if (delivery.Status != "Sent") throw new InvalidOperationException("Delivery remains pending.");
        }, ct);
    }

    private async Task<NotificationType> GetOrCreateNotificationTypeAsync(string? typeCode, CancellationToken ct = default)
    {
        var code = string.IsNullOrWhiteSpace(typeCode) ? GeneralTypeCode : typeCode.Trim().ToUpperInvariant();
        var type = await _repository.GetTypeAsync(code, ct);
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

        await _repository.AddTypeAsync(type, ct);
        return type;
    }

    private async Task<List<NotificationChannel>> ResolveChannelsAsync(NotificationRequest request, int notificationTypeId, CancellationToken ct = default)
    {
        var channels = request.Channels?.Count > 0
            ? request.Channels.Distinct().ToList()
            : await GetDefaultChannelsAsync(notificationTypeId, ct);

        var preference = await _repository.GetPreferenceAsync(request.UserId, notificationTypeId, ct);

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

    private async Task<List<NotificationChannel>> GetDefaultChannelsAsync(int notificationTypeId, CancellationToken ct = default)
    {
        var defaultChannels = await _repository.GetDefaultChannelsAsync(notificationTypeId, ct);

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

    private Task<NotificationTemplate?> GetTemplateAsync(int notificationTypeId, NotificationChannel channel, CancellationToken ct = default)
    {
        return _repository.GetTemplateAsync(notificationTypeId, channel.ToString(), ct);
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




