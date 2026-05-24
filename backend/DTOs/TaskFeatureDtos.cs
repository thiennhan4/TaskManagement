using System.ComponentModel.DataAnnotations;
using TaskHub.backend.Models;

namespace TaskHub.backend.DTOs;

public class ChangeTaskStatusDto
{
    [Required]
    public TaskItemStatus NewStatus { get; set; }
}

public class AssignTaskDto
{
    public Guid? AssignedToUserId { get; set; }
}

public class TaskFilterDto
{
    public TaskItemStatus? Status { get; set; }
    public TaskItemPriority? Priority { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? ListId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? SearchKeyword { get; set; }
    public bool? IsOverdue { get; set; }
    public string? SortBy { get; set; } // CreatedAt, DueDate, Priority, Title
    public string? SortOrder { get; set; } // asc, desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class TaskDetailResponseDto : TaskResponseDto
{
    public List<CommentResponseDto> Comments { get; set; } = new();
    public List<AttachmentResponseDto> Attachments { get; set; } = new();
    public List<ActivityLogResponseDto> ActivityLogs { get; set; } = new();
}

public class AttachmentResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = null!;
    public string UploadedByUserName { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public string FileUrl { get; set; } = null!;
    public bool CanDelete { get; set; }
}

public class ActivityLogResponseDto
{
    public string Action { get; set; } = null!;
    public string ActionDescription { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string UserName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class PagedTaskResponseDto
{
    public List<TaskResponseDto> Tasks { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class TaskCalendarDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskItemPriority Priority { get; set; }
    public string? BoardName { get; set; }
    public string? ProjectName { get; set; }
    public string? Color { get; set; } // Color from project or board
}

public class CalendarFilterDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? BoardId { get; set; }
}

public class InviteTaskMemberDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}

public class AcceptTaskInvitationDto
{
    [Required]
    public string Token { get; set; } = null!;
}
