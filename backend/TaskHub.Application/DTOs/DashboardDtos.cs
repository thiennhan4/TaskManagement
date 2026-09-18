namespace TaskHub.Application.DTOs;

/// <summary>
/// Typed response for dashboard statistics â€” replaces anonymous objects.
/// </summary>
public enum DashboardScope
{
    Personal,
    Team
}

public enum DashboardTimeframe
{
    Week,
    Month,
    SixMonths,
    Year
}

public class DashboardQueryDto
{
    public string? Scope { get; set; } = nameof(DashboardScope.Personal);
    public Guid? TeamId { get; set; }
    public string? Timeframe { get; set; } = nameof(DashboardTimeframe.SixMonths);
}

public class DashboardScopeCriteria
{
    public Guid UserId { get; set; }
    public DashboardScope Scope { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? ProjectId { get; set; }
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

public class DashboardVelocityPointDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int Created { get; set; }
    public int Completed { get; set; }
}

public class DashboardAnalyticsSnapshotDto
{
    public List<DashboardAnalyticsTaskDto> Tasks { get; set; } = new();
    public List<DashboardCompletionDto> Completions { get; set; } = new();
    public int TimeTrackedSeconds { get; set; }
}

public class DashboardAnalyticsTaskDto
{
    public Guid Id { get; set; }
    public TaskHub.Domain.Entities.TaskItemStatus Status { get; set; }
    public TaskHub.Domain.Entities.TaskItemPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
}

public class DashboardCompletionDto
{
    public Guid TaskId { get; set; }
    public DateTime CompletedAt { get; set; }
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

public class DashboardActivityDto
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = "";
    public string EventType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TeamId { get; set; }
    public string ProjectName { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}



