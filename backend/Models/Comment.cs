namespace TaskHub.backend.Models;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = null!;
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
