using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
namespace TaskHub.Infrastructure.Repositories;
public sealed class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public Task<AppUser?> GetRecipientAsync(Guid userId, CancellationToken ct) => db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    public Task<NotificationType?> GetTypeAsync(string code, CancellationToken ct) => db.NotificationTypes.FirstOrDefaultAsync(t => t.Code == code, ct);
    public async Task AddTypeAsync(NotificationType type, CancellationToken ct) { db.NotificationTypes.Add(type); await db.SaveChangesAsync(ct); }
    public Task<UserNotificationPreference?> GetPreferenceAsync(Guid userId, int typeId, CancellationToken ct) => db.UserNotificationPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationTypeId == typeId, ct);
    public Task<NotificationTemplate?> GetTemplateAsync(int typeId, string channel, CancellationToken ct) => db.NotificationTemplates.AsNoTracking().Where(t => t.NotificationTypeId == typeId && t.Channel == channel && t.IsActive).OrderByDescending(t => t.Version).FirstOrDefaultAsync(ct);
    public Task<string?> GetDefaultChannelsAsync(int typeId, CancellationToken ct) => db.NotificationTypes.Where(t => t.Id == typeId).Select(t => t.DefaultChannels).FirstOrDefaultAsync(ct);
    public Task<List<Notification>> GetPageAsync(Guid userId, int page, int size, bool unreadOnly, CancellationToken ct) => db.Notifications.AsNoTracking().Include(n => n.NotificationType)
        .Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead) && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow))
        .OrderByDescending(n => n.CreatedAt).ThenBy(n => n.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct) => db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow), ct);
    public async Task<bool> MarkReadAsync(Guid userId, int? id, CancellationToken ct)
    {
        var rows = await db.Notifications.Where(n => n.UserId == userId && !n.IsRead && (!id.HasValue || n.Id == id)).ToListAsync(ct);
        foreach (var row in rows) { row.IsRead = true; row.ReadAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
        return rows.Count > 0;
    }
    public async Task PersistAsync(Notification notification, CancellationToken ct) { db.Notifications.Add(notification); await db.SaveChangesAsync(ct); }
    public async Task AddDeliveryAsync(NotificationDelivery delivery, CancellationToken ct) { db.NotificationDeliveries.Add(delivery); await db.SaveChangesAsync(ct); }
    public async Task SaveDeliveryAsync(NotificationDelivery delivery, CancellationToken ct) { await db.SaveChangesAsync(ct); }
}
