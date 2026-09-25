using TaskHub.Domain.Entities;

namespace TaskHub.Application.DTOs;

public class PageQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
public class KanbanQueryDto : PageQueryDto
{
    public Guid? BoardId { get; set; }
    public Guid? ListId { get; set; }
    public int TaskPage { get; set; } = 1;
    public int TaskPageSize { get; set; } = 50;
}
public class BoardSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Color { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
public class KanbanColumnDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = "";
    public string? Color { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public PagedResult<TaskResponseDto> Tasks { get; set; } = new() { Page = 1, PageSize = 50 };
}
public class ProjectKanbanResponseDto
{
    public BoardSummaryDto? Board { get; set; }
    public PagedResult<KanbanColumnDto> Lists { get; set; } = new() { Page = 1, PageSize = 20 };
    public bool CanManageColumns { get; set; }
    public bool CanCreateTasks { get; set; }
}
public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string? AvatarUrl { get; set; }
}
public class ProjectActivityResponseDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectActivityAction Action { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserSummaryDto User { get; set; } = new();
}

public class TransferOwnershipDto { public Guid TargetUserId { get; set; } }
