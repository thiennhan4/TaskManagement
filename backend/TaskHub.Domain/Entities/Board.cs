namespace TaskHub.Domain.Entities;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid? ProjectId { get; set; }  // optional link to Project

    // Navigation properties
    public AppUser Owner { get; set; } = null!;
    public Project? Project { get; set; }
    public ICollection<BoardList> Lists { get; set; } = new List<BoardList>();
}



