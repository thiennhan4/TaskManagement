using System.ComponentModel.DataAnnotations.Schema;

namespace TaskHub.backend.Models;

[Table("Tasks")]
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid ListId { get; set; }
    public Guid OwnerId { get; set; }
    public int Position { get; set; }
    public string Status { get; set; } = "Todo"; // Todo, Doing, Done
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Label { get; set; }
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public BoardList List { get; set; } = null!;
    public AppUser Owner { get; set; } = null!;
}
