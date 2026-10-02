using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface ITaskItemRepository
{
    Task<IEnumerable<TaskItem>> GetTasksByListIdAsync(Guid listId, CancellationToken ct = default);
    Task<TaskItem?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<TaskItem> Tasks, int TotalCount)> GetTasksAsync(TaskHub.Application.DTOs.TaskFilterDto filter, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<TaskItem> CreateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task MoveWithinBoardAsync(TaskItem task, BoardList destination, int position, CancellationToken ct = default);
    Task UpdateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task AddActivityLogAsync(TaskActivityLog log, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<bool> CanUserAccessTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<TaskItem?> GetTaskForAuthorizationAsync(Guid taskId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetCalendarTasksAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null, Guid? boardId = null, CancellationToken ct = default);
}




