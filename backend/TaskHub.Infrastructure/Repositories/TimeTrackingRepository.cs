using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
namespace TaskHub.Infrastructure.Repositories;
public class TimeTrackingRepository(AppDbContext db) : ITimeTrackingRepository
{
    public Task<TimeEntry?> GetAsync(Guid id) => db.TimeEntries.FirstOrDefaultAsync(e=>e.Id==id);
    public Task<TimeEntry?> GetRunningAsync(Guid userId) => db.TimeEntries.FirstOrDefaultAsync(e=>e.UserId==userId && e.EndTime==null);
    public async Task AddAsync(TimeEntry entry) { db.TimeEntries.Add(entry); await db.SaveChangesAsync(); }
    public async Task SaveAsync() => await db.SaveChangesAsync();
    public async Task DeleteAsync(TimeEntry entry) { db.TimeEntries.Remove(entry); await db.SaveChangesAsync(); }
    public async Task<PagedResult<TimeEntryDto>> GetTaskPageAsync(Guid taskId, PageQueryDto query)
    {
        var entries=db.TimeEntries.AsNoTracking().Where(e=>e.TaskId==taskId);
        return new() { Page=query.Page, PageSize=query.PageSize, TotalItems=await entries.CountAsync(),
            Items=await entries.OrderByDescending(e=>e.StartTime).ThenBy(e=>e.Id).Skip((query.Page-1)*query.PageSize).Take(query.PageSize)
                .Select(e=>new TimeEntryDto { Id=e.Id, TaskId=e.TaskId, TaskTitle=e.Task.Title, UserId=e.UserId, UserName=e.User.FullName,
                    StartTime=e.StartTime, EndTime=e.EndTime, DurationSeconds=e.DurationSeconds, Description=e.Description, IsBillable=e.IsBillable, CreatedAt=e.CreatedAt }).ToListAsync() };
    }
    public Task<List<TimeEntry>> GetUserEntriesAsync(Guid userId, DateTime? from, DateTime? to, Guid? boardId)
    {
        var entries=db.TimeEntries.AsNoTracking().Include(e=>e.Task).Include(e=>e.User).Where(e=>e.UserId==userId && !e.Task.IsDeleted);
        if(from.HasValue) entries=entries.Where(e=>e.StartTime>=from.Value);
        if(to.HasValue) entries=entries.Where(e=>e.StartTime<=to.Value);
        if(boardId.HasValue) entries=entries.Where(e=>e.Task.List.BoardId==boardId.Value);
        return entries.OrderByDescending(e=>e.StartTime).ToListAsync();
    }
}
