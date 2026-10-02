using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface ITaskItemRepository
{
    Task<TaskHub.Application.DTOs.PagedResult<TaskHub.Application.DTOs.TaskResponseDto>> GetPageAsync(TaskHub.Application.DTOs.TaskFilterDto filter, Guid user, bool admin, CancellationToken ct = default);
    Task<TaskHub.Application.DTOs.TaskSummaryDto> GetSummaryAsync(Guid user, bool admin, CancellationToken ct = default);
    Task<TaskHub.Application.DTOs.TaskResponseDto?> GetHeaderAsync(Guid id, CancellationToken ct = default);
    Task<TaskHub.Application.DTOs.PagedResult<TaskHub.Application.DTOs.TaskCalendarDto>> GetCalendarPageAsync(TaskHub.Application.DTOs.CalendarFilterDto filter, Guid user, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetTasksByListIdAsync(Guid listId, CancellationToken ct = default);
    Task<TaskItem?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<TaskItem> Tasks, int TotalCount)> GetTasksAsync(TaskHub.Application.DTOs.TaskFilterDto filter, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<TaskHub.Application.DTOs.PagedResult<TaskHub.Application.DTOs.TaskResponseDto>> GetByUserIdAsync(Guid userId, TaskHub.Application.DTOs.TaskFilterDto query, CancellationToken ct = default);
    Task<TaskItem> CreateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task MoveWithinBoardAsync(TaskItem task, BoardList destination, int position, CancellationToken ct = default);
    Task UpdateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task AddActivityLogAsync(TaskActivityLog log, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<bool> CanUserAccessTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<TaskItem?> GetTaskForAuthorizationAsync(Guid taskId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetCalendarTasksAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null, Guid? boardId = null, CancellationToken ct = default);
}




