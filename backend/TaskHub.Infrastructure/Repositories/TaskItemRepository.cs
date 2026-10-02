using TaskHub.Application.Services;
﻿using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;

namespace TaskHub.Infrastructure.Repositories;

public partial class TaskItemRepository : ITaskItemRepository
{
    private readonly AppDbContext _context;

    public TaskItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddActivityLogAsync(TaskActivityLog log, CancellationToken ct = default)
    {
        _context.TaskActivityLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByListIdAsync(Guid listId, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Where(t => t.ListId == listId && !t.IsDeleted)
            .OrderBy(t => t.Position).ThenBy(t => t.Id)
            .Take(100)
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
            .Include(t => t.List).ThenInclude(l => l.Board).ThenInclude(b => b.Project)
            .Include(t => t.Owner)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }

    public async Task<(IEnumerable<TaskItem> Tasks, int TotalCount)> GetTasksAsync(TaskHub.Application.DTOs.TaskFilterDto filter, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var query = Filter(filter, userId, isAdmin);
        var totalCount = await query.CountAsync(ct);
        var tasks = await Order(query, filter).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        return (tasks, totalCount);
    }

    public async Task<IEnumerable<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Where(t => t.List.Board.ProjectId == projectId && !t.IsDeleted)
            .ToListAsync(ct);
    }

    public Task<TaskHub.Application.DTOs.PagedResult<TaskHub.Application.DTOs.TaskResponseDto>> GetByUserIdAsync(Guid userId, TaskHub.Application.DTOs.TaskFilterDto query, CancellationToken ct = default) =>
        GetPageAsync(query, userId, false, ct);

    public async Task<TaskItem> CreateTaskAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    public async Task MoveWithinBoardAsync(TaskItem task, BoardList destination, int position, CancellationToken ct = default)
    {
        var sourceId = task.ListId;
        var siblings = await _context.Tasks.Where(t => !t.IsDeleted && (t.ListId == sourceId || t.ListId == destination.Id))
            .OrderBy(t => t.Position).ThenBy(t => t.Id).ToListAsync(ct);
        var target = siblings.Where(t => t.ListId == destination.Id && t.Id != task.Id).ToList();
        target.Insert(Math.Min(position, target.Count), task);
        task.ListId = destination.Id;
        task.List = destination;
        var now = DateTime.UtcNow;
        for (var i = 0; i < target.Count; i++) { target[i].Position = i; target[i].UpdatedAt = now; }
        if (sourceId != destination.Id)
        {
            var source = siblings.Where(t => t.ListId == sourceId && t.Id != task.Id).ToList();
            for (var i = 0; i < source.Count; i++) { source[i].Position = i; source[i].UpdatedAt = now; }
        }
        // Status is independent of user-editable column names; use the dedicated status action.
        await _context.SaveChangesAsync(ct);
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
            .Include(t => t.List).ThenInclude(l => l.Board).ThenInclude(b => b.Project)
            .Include(t => t.Team).ThenInclude(tm => tm!.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
            
        if (task == null) return false;

        if (task.OwnerId == userId || task.AssignedToId == userId) return true;

        if (task.List.Board.OwnerId == userId) return true;
        
        if (task.Team != null && task.Team.Members.Any(m => m.UserId == userId)) return true;

        return false;
    }

    public async Task<TaskItem?> GetTaskForAuthorizationAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
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

        query = query.Where(ResourceVisibility.Tasks(userId));

        if (projectId.HasValue)
            query = query.Where(t => t.List.Board.ProjectId == projectId.Value);
            
        if (boardId.HasValue)
            query = query.Where(t => t.ListId == boardId.Value || t.List.BoardId == boardId.Value);

        return await query.ToListAsync(ct);
    }
}




