using TaskHub.Domain.Entities;
namespace TaskHub.Application.Repositories.Interfaces;
public interface INotificationRepository
{
    Task<AppUser?> GetRecipientAsync(Guid userId, CancellationToken ct);
    Task<NotificationType?> GetTypeAsync(string code, CancellationToken ct);
    Task AddTypeAsync(NotificationType type, CancellationToken ct);
    Task<UserNotificationPreference?> GetPreferenceAsync(Guid userId, int typeId, CancellationToken ct);
    Task<NotificationTemplate?> GetTemplateAsync(int typeId, string channel, CancellationToken ct);
    Task<string?> GetDefaultChannelsAsync(int typeId, CancellationToken ct);
    Task<List<Notification>> GetPageAsync(Guid userId, int page, int size, bool unreadOnly, CancellationToken ct);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct);
    Task<int> GetCountAsync(Guid userId, CancellationToken ct);
    Task<bool> MarkReadAsync(Guid userId, int? id, CancellationToken ct);
    Task PersistAsync(Notification notification, CancellationToken ct);
    Task AddDeliveryAsync(NotificationDelivery delivery, CancellationToken ct);
    Task SaveDeliveryAsync(NotificationDelivery delivery, CancellationToken ct);
}
