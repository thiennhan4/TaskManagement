using TaskHub.backend.DTOs;

namespace TaskHub.backend.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, string title, string message, string? linkUrl = null);
    Task<NotificationResult> SendAsync(NotificationRequest request);
    Task<BatchNotificationResult> SendBatchAsync(BatchNotificationRequest request);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20, bool unreadOnly = false);
    Task MarkAsReadAsync(int notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
}
