namespace TaskHub.Domain.Entities;

public class TaskInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string InviteeEmail { get; set; } = null!;
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsAccepted { get; set; } = false;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }

    // Navigation
    public TaskItem Task { get; set; } = null!;
    public AppUser InvitedByUser { get; set; } = null!;
}



