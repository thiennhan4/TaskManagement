using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
namespace TaskHub.Application.Repositories.Interfaces;
public interface ITimeTrackingRepository
{
    Task<TimeEntry?> GetAsync(Guid id);
    Task<TimeEntry?> GetRunningAsync(Guid userId);
    Task AddAsync(TimeEntry entry);
    Task SaveAsync();
    Task DeleteAsync(TimeEntry entry);
    Task<PagedResult<TimeEntryDto>> GetTaskPageAsync(Guid taskId, PageQueryDto query);
    Task<List<TimeEntry>> GetUserEntriesAsync(Guid userId, DateTime? from, DateTime? to, Guid? boardId);
}
