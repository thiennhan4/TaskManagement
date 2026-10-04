using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

public interface ITaskItemService
{
    Task<PagedResult<EligibleAssigneeDto>> GetEligibleAssigneesAsync(Guid taskId, Guid userId, PageQueryDto query, CancellationToken ct = default);
    Task<PagedResult<TaskResponseDto>> GetPageAsync(TaskFilterDto filter, Guid userId, CancellationToken ct = default);
    Task<TaskSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResult<TaskCalendarDto>> GetCalendarPageAsync(CalendarFilterDto filter, Guid userId, CancellationToken ct = default);
    Task<PagedTaskResponseDto> GetTasksAsync(TaskFilterDto filter, Guid userId, CancellationToken ct = default);
    Task<List<TaskResponseDto>> GetTasksByListAsync(Guid listId, Guid userId, CancellationToken ct = default);
    Task<TaskDetailResponseDto> GetTaskByIdAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<TaskResponseDto> CreateTaskAsync(Guid listId, CreateTaskDto dto, Guid userId, CancellationToken ct = default);
    Task<TaskResponseDto> CreatePersonalTaskAsync(CreateTaskDto dto, Guid userId, CancellationToken ct = default);
    Task<TaskResponseDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid userId, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<TaskResponseDto> ChangeStatusAsync(Guid taskId, ChangeTaskStatusDto dto, Guid userId, CancellationToken ct = default);
    Task<TaskResponseDto> AssignTaskAsync(Guid taskId, AssignTaskDto dto, Guid userId, CancellationToken ct = default);
    Task<TaskResponseDto> MoveTaskAsync(Guid taskId, MoveTaskDto dto, Guid userId, CancellationToken ct = default);
    Task<TaskResponseDto> UpdateProgressAsync(Guid taskId, UpdateProgressDto dto, Guid userId, CancellationToken ct = default);
    Task<List<TaskResponseDto>> GetMyTasksAsync(Guid userId, CancellationToken ct = default);
    Task<List<TaskCalendarDto>> GetCalendarTasksAsync(CalendarFilterDto filter, Guid userId, CancellationToken ct = default);
    
    // Attachments
    Task<TaskHub.Application.Models.AttachmentDownload> DownloadAttachmentAsync(Guid taskId, Guid attachmentId, Guid userId, CancellationToken ct = default);
    Task<AttachmentResponseDto> UploadAttachmentAsync(Guid taskId, Microsoft.AspNetCore.Http.IFormFile file, Guid userId, CancellationToken ct = default);
    Task<List<AttachmentResponseDto>> GetAttachmentsAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task DeleteAttachmentAsync(Guid attachmentId, Guid userId, CancellationToken ct = default, Guid? taskId = null);

    // Activity Logs
    Task<List<ActivityLogResponseDto>> GetActivityLogsAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    // Task Invitations
    Task InviteMemberToTaskAsync(Guid taskId, InviteTaskMemberDto dto, Guid currentUserId, CancellationToken ct = default);
    Task AcceptTaskInvitationAsync(AcceptTaskInvitationDto dto, Guid currentUserId, CancellationToken ct = default);
}





