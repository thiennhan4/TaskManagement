using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, string title, string message, string? linkUrl = null, CancellationToken ct = default);
    Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken ct = default);
    Task<BatchNotificationResult> SendBatchAsync(BatchNotificationRequest request, CancellationToken ct = default);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20, bool unreadOnly = false, CancellationToken ct = default);
    Task MarkAsReadAsync(int notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
}





