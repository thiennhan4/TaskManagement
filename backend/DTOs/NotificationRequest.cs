namespace TaskHub.backend.DTOs;

public enum NotificationChannel
{
    Email,
    InApp,
    SMS,
    Push
}

public class NotificationRequest
{
    public Guid UserId { get; set; }
    public string TypeCode { get; set; } = "GENERAL";
    public List<NotificationChannel>? Channels { get; set; }

    public string? Title { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Data { get; set; }

    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class BatchNotificationRequest
{
    public List<NotificationRequest> Notifications { get; set; } = new();
}

public class NotificationResult
{
    public bool Success { get; set; }
    public int? NotificationId { get; set; }
    public string? ErrorMessage { get; set; }

    public static NotificationResult Ok(int notificationId) => new()
    {
        Success = true,
        NotificationId = notificationId
    };

    public static NotificationResult Fail(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}

public class BatchNotificationResult
{
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public List<NotificationResult> Results { get; set; } = new();
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? ActionUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? ActionLabel { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}
