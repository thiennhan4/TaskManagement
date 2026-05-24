namespace TaskHub.backend.DTOs;

/// <summary>
/// Typed response for dashboard statistics — replaces anonymous objects.
/// </summary>
public class DashboardStatsDto
{
    public int Total { get; set; }
    public int Todo { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int Overdue { get; set; }
    public int TotalBoards { get; set; }
    public int TeamMembers { get; set; }
}

/// <summary>
/// Typed response for recent/upcoming task cards on dashboard.
/// </summary>
public class DashboardTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int Progress { get; set; }
    public Guid ListId { get; set; }
    public string? ListName { get; set; }
    public DateTime CreatedAt { get; set; }
}
