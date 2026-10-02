using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
namespace TaskHub.Application.Repositories.Interfaces;
public interface ITimeTrackingRepository
{
    Task<TimeEntry?> GetAsync(Guid id, CancellationToken ct = default);
    Task<TimeEntry?> GetRunningAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(TimeEntry entry, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
    Task DeleteAsync(TimeEntry entry, CancellationToken ct = default);
    Task<PagedResult<TimeEntryDto>> GetTaskPageAsync(Guid taskId, PageQueryDto query, CancellationToken ct = default);
    Task<List<TimeEntry>> GetUserEntriesAsync(Guid userId, DateTime? from, DateTime? to, Guid? boardId, CancellationToken ct = default);
}
