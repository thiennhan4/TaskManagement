using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskHub.Domain.Entities;

public class NotificationType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? DefaultChannels { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<NotificationTemplate> Templates { get; set; } = new List<NotificationTemplate>();
    public ICollection<UserNotificationPreference> UserPreferences { get; set; } = new List<UserNotificationPreference>();
}

public class NotificationTemplate
{
    public int Id { get; set; }
    public int NotificationTypeId { get; set; }

    [MaxLength(20)]
    public string Channel { get; set; } = null!;

    [MaxLength(200)]
    public string? Subject { get; set; }

    public string? BodyTemplate { get; set; }

    [MaxLength(200)]
    public string? TitleTemplate { get; set; }

    public string? MessageTemplate { get; set; }

    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public NotificationType NotificationType { get; set; } = null!;
}

public class UserNotificationPreference
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int NotificationTypeId { get; set; }

    public bool AllowEmail { get; set; } = true;
    public bool AllowInApp { get; set; } = true;
    public bool AllowSMS { get; set; }
    public bool AllowPush { get; set; }

    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public AppUser User { get; set; } = null!;
    public NotificationType NotificationType { get; set; } = null!;
}

public class Notification
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public int NotificationTypeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    public string Message { get; set; } = null!;

    public string? Data { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    [MaxLength(50)]
    public string? ActionLabel { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public AppUser User { get; set; } = null!;
    public NotificationType NotificationType { get; set; } = null!;
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();

    [NotMapped]
    public string? LinkUrl
    {
        get => ActionUrl;
        set => ActionUrl = value;
    }
}

public class NotificationDelivery
{
    public long Id { get; set; }
    public int NotificationId { get; set; }

    [MaxLength(20)]
    public string Channel { get; set; } = null!;

    [MaxLength(255)]
    public string? RecipientEmail { get; set; }

    [MaxLength(20)]
    public string? RecipientPhone { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? StatusMessage { get; set; }

    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }

    [MaxLength(255)]
    public string? ProviderMessageId { get; set; }

    [MaxLength(50)]
    public string? ProviderName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Notification Notification { get; set; } = null!;
    public ICollection<EmailTrackingToken> EmailTrackingTokens { get; set; } = new List<EmailTrackingToken>();
}

public class EmailTrackingToken
{
    public Guid Token { get; set; } = Guid.NewGuid();
    public long DeliveryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public NotificationDelivery Delivery { get; set; } = null!;
}



