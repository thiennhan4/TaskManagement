namespace TaskHub.Application.DTOs;

public class TaskSummaryDto
{
    public int Total { get; set; }
    public int Todo { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int Review { get; set; }
}

public class ProjectQueryDto : PageQueryDto
{
    public Guid? WorkspaceId { get; set; }
    public bool IncludeArchived { get; set; }
    public bool ArchivedOnly { get; set; }
    public string? Search { get; set; }
    public TaskHub.Domain.Entities.ProjectStatus? Status { get; set; }
}

public class TimeQueryDto : PageQueryDto
{
    public DateTime From { get; set; } = DateTime.UtcNow.Date.AddDays(-30);
    public DateTime To { get; set; } = DateTime.UtcNow;
    public Guid? BoardId { get; set; }
}

public class NotificationQueryDto : PageQueryDto
{
    public bool UnreadOnly { get; set; }
}
