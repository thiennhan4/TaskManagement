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
            .Where(t => t.ListId == listId && !t.IsDeleted)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);
    }

    public async Task<TaskItem?> GetTaskByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.List)
            .ThenInclude(l => l.Board)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }

    public async Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.List).ThenInclude(l => l.Board)
            .Include(t => t.Owner)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments.Where(c => !c.IsDeleted)).ThenInclude(c => c.User)
            .Include(t => t.Attachments).ThenInclude(a => a.UploadedByUser)
            .Include(t => t.ActivityLogs).ThenInclude(al => al.User)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }

    public async Task<(IEnumerable<TaskItem> Tasks, int TotalCount)> GetTasksAsync(TaskHub.backend.DTOs.TaskFilterDto filter, CancellationToken ct = default)
    {
        var query = _context.Tasks
            .Include(t => t.List).ThenInclude(l => l.Board)
            .Include(t => t.Owner)
            .Include(t => t.AssignedTo)
            .Where(t => !t.IsDeleted).AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);
        
        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);

        if (filter.BoardId.HasValue)
            query = query.Where(t => t.List.BoardId == filter.BoardId.Value);

        if (filter.ListId.HasValue)
            query = query.Where(t => t.ListId == filter.ListId.Value);

        if (filter.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToId == filter.AssignedToUserId.Value);

        if (!string.IsNullOrEmpty(filter.SearchKeyword))
        {
            var keyword = filter.SearchKeyword.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(keyword) || (t.Description != null && t.Description.ToLower().Contains(keyword)));
        }

        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskItemStatus.Done);
        }

        var totalCount = await query.CountAsync(ct);

        query = filter.SortBy?.ToLower() switch
        {
            "duedate" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "priority" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "title" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            _ => filter.SortOrder?.ToLower() == "asc" ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt)
        };

        var tasks = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (tasks, totalCount);
    }

    public async Task<IEnumerable<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.List)
            .Where(t => t.List.BoardId == projectId && !t.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.List).ThenInclude(l => l.Board)
            .Include(t => t.Owner)
            .Include(t => t.AssignedTo)
            .Where(t => (t.OwnerId == userId || t.AssignedToId == userId) && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    public async Task UpdateTaskAsync(TaskItem task, CancellationToken ct = default)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _context.Entry(task).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteTaskAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await _context.Tasks.FindAsync(new object[] { id }, ct);
        if (task != null)
        {
            task.IsDeleted = true;
            task.DeletedAt = DateTime.UtcNow;
            
            // Soft delete
            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> CanUserAccessTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await _context.Tasks
            .Include(t => t.List).ThenInclude(l => l.Board)
            .Include(t => t.Team).ThenInclude(tm => tm!.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
            
        if (task == null) return false;

        if (task.OwnerId == userId || task.AssignedToId == userId) return true;

        if (task.List.Board.OwnerId == userId) return true;
        
        if (task.Team != null && task.Team.Members.Any(m => m.UserId == userId)) return true;

        return false;
    }

    public async Task<IEnumerable<TaskItem>> GetCalendarTasksAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null, Guid? boardId = null, CancellationToken ct = default)
    {
        var query = _context.Tasks
            .Include(t => t.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Project)
            .Where(t => !t.IsDeleted && 
                        ((t.DueDate >= start && t.DueDate <= end) || 
                         (t.StartDate >= start && t.StartDate <= end) ||
                         (t.StartDate <= start && t.DueDate >= end)))
            .AsQueryable();

        // Security: Filter by the user's tasks or tasks in projects they are members of
        query = query.Where(t => t.AssignedToId == userId || 
                                 t.OwnerId == userId ||
                                 t.List.Board.OwnerId == userId ||
                                 _context.ProjectMembers.Any(pm => pm.ProjectId == t.List.Board.ProjectId && pm.UserId == userId));

        if (projectId.HasValue)
            query = query.Where(t => t.List.Board.ProjectId == projectId.Value);
            
        if (boardId.HasValue)
            query = query.Where(t => t.ListId == boardId.Value || t.List.BoardId == boardId.Value);

        return await query.ToListAsync(ct);
    }
}
