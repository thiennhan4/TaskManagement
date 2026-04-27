namespace TaskHub.backend.Models;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public AppUser Owner { get; set; } = null!;
    public ICollection<BoardList> Lists { get; set; } = new List<BoardList>();
}
