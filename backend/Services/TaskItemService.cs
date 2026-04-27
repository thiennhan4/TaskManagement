using TaskHub.backend.Models;
using TaskHub.backend.Repositories.Interfaces;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

public class TaskItemService : ITaskItemService
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IBoardListRepository _listRepository;
    private readonly IBoardRepository _boardRepository;

    public TaskItemService(ITaskItemRepository taskRepository, IBoardListRepository listRepository, IBoardRepository boardRepository)
    {
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _boardRepository = boardRepository;
    }

    private async Task<bool> IsUserBoardOwner(Guid listId, Guid userId, CancellationToken ct)
    {
        var list = await _listRepository.GetListByIdAsync(listId, ct);
        if (list == null) return false;

        var board = await _boardRepository.GetBoardByIdAsync(list.BoardId, ct);
        return board != null && board.OwnerId == userId;
    }

    public async Task<IEnumerable<TaskItem>> GetTasksAsync(Guid listId, Guid userId, CancellationToken ct = default)
    {
        if (!await IsUserBoardOwner(listId, userId, ct)) return new List<TaskItem>();
        return await _taskRepository.GetTasksByListIdAsync(listId, ct);
    }

    public async Task<TaskItem?> CreateTaskAsync(TaskItem task, Guid userId, CancellationToken ct = default)
    {
        if (!await IsUserBoardOwner(task.ListId, userId, ct)) return null;

        task.CreatedAt = DateTime.UtcNow;
        return await _taskRepository.CreateTaskAsync(task, ct);
    }

    public async Task<bool> UpdateTaskAsync(Guid taskId, TaskItem updatedTask, Guid userId, CancellationToken ct = default)
    {
        var existingTask = await _taskRepository.GetTaskByIdAsync(taskId, ct);
        if (existingTask == null) return false;
        if (existingTask.List.Board.OwnerId != userId) return false;

        existingTask.Title = updatedTask.Title;
        existingTask.Description = updatedTask.Description;
        existingTask.Position = updatedTask.Position;
        existingTask.Priority = updatedTask.Priority;
        existingTask.DueDate = updatedTask.DueDate;
        existingTask.Label = updatedTask.Label;
        existingTask.Progress = updatedTask.Progress;
        existingTask.UpdatedAt = DateTime.UtcNow;

        if (updatedTask.ListId != Guid.Empty && existingTask.ListId != updatedTask.ListId)
        {
            if (!await IsUserBoardOwner(updatedTask.ListId, userId, ct)) return false;
            existingTask.ListId = updatedTask.ListId;
        }

        await _taskRepository.UpdateTaskAsync(existingTask, ct);
        return true;
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var existingTask = await _taskRepository.GetTaskByIdAsync(taskId, ct);
        if (existingTask == null) return false;
        if (existingTask.List.Board.OwnerId != userId) return false;

        await _taskRepository.DeleteTaskAsync(taskId, ct);
        return true;
    }
}
