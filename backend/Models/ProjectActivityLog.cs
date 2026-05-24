namespace TaskHub.backend.Models;

public class ProjectActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectActivityAction Action { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }  // JSON blob for extra context
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Project Project { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
