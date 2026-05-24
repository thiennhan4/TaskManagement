namespace TaskHub.backend.Models;

public class ProjectInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string InviteeEmail { get; set; } = null!;
    public ProjectRole Role { get; set; } = ProjectRole.Member;
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsAccepted { get; set; } = false;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
    public AppUser InvitedByUser { get; set; } = null!;
}
