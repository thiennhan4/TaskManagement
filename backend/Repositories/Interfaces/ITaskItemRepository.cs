using TaskHub.backend.Models;

namespace TaskHub.backend.Repositories.Interfaces;

public interface ITaskItemRepository
{
    Task<IEnumerable<TaskItem>> GetTasksByListIdAsync(Guid listId, CancellationToken ct = default);
    Task<TaskItem?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItem> CreateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateTaskAsync(TaskItem task, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid id, CancellationToken ct = default);
}
