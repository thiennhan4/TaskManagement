using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface ITaskItemRepository
{
    Task<IEnumerable<TaskItem>> GetTasksByListIdAsync(Guid listId, CancellationToken ct = default);
    Task<TaskItem?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<TaskItem> Tasks, int TotalCount)> GetTasksAsync(TaskHub.Application.DTOs.TaskFilterDto filter, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<TaskItem> CreateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<bool> CanUserAccessTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetCalendarTasksAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null, Guid? boardId = null, CancellationToken ct = default);
}




