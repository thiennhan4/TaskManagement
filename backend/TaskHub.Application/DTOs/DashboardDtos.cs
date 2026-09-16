namespace TaskHub.Application.DTOs;

/// <summary>
/// Typed response for dashboard statistics â€” replaces anonymous objects.
/// </summary>
public enum DashboardScope
{
    Personal,
    Team
}

public class DashboardQueryDto
{
    public string? Scope { get; set; } = nameof(DashboardScope.Personal);
    public Guid? TeamId { get; set; }
}

public class DashboardScopeCriteria
{
    public Guid UserId { get; set; }
    public DashboardScope Scope { get; set; }
    public Guid? TeamId { get; set; }
}

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



