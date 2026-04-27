using TaskHub.backend.Models;

namespace TaskHub.backend.Services.Interfaces;

public interface ITaskItemService
{
    Task<IEnumerable<TaskItem>> GetTasksAsync(Guid listId, Guid userId, CancellationToken ct = default);
    Task<TaskItem?> CreateTaskAsync(TaskItem task, Guid userId, CancellationToken ct = default);
    Task<bool> UpdateTaskAsync(Guid taskId, TaskItem updatedTask, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);
}
