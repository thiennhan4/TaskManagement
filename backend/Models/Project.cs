namespace TaskHub.backend.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? Emoji { get; set; } = "📁";
    public string? CoverImageUrl { get; set; }
    public string? Color { get; set; } = "#6366f1";
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;
    public int ProjectType { get; set; } = 1; // 1 = Personal, 2 = Team

    // Owner + Workspace (Team acts as workspace)
    public Guid OwnerId { get; set; }
    public Guid? WorkspaceId { get; set; }  // FK → Team.Id, Null means Personal Project

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // Computed helper
    public bool IsArchived => ArchivedAt.HasValue;

    // Navigation
    public AppUser Owner { get; set; } = null!;
    public Team? Workspace { get; set; }
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<ProjectInvitation> Invitations { get; set; } = new List<ProjectInvitation>();
    public ICollection<ProjectActivityLog> ActivityLogs { get; set; } = new List<ProjectActivityLog>();
    public ICollection<Board> Boards { get; set; } = new List<Board>();
}
