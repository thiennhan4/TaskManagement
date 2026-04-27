using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.Models;
using TaskHub.backend.Repositories.Interfaces;

namespace TaskHub.backend.Repositories;

public class TaskItemRepository : ITaskItemRepository
{
    private readonly AppDbContext _context;

    public TaskItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByListIdAsync(Guid listId, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Where(t => t.ListId == listId)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);
    }

    public async Task<TaskItem?> GetTaskByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.List)
            .ThenInclude(l => l.Board)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    public async Task UpdateTaskAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Entry(task).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteTaskAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _context.Tasks.FindAsync(new object[] { id }, ct);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync(ct);
        }
    }
}
