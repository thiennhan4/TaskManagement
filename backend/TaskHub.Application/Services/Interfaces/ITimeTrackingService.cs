using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

public interface ITimeTrackingService
{
    Task<PagedResult<TimeEntryDto>> GetUserPageAsync(Guid userId, TimeQueryDto query, CancellationToken ct = default);
    Task<TimeReportDto> GetReportPageAsync(Guid userId, TimeQueryDto query, CancellationToken ct = default);
    Task<TimeEntryDto> StartTimerAsync(Guid userId, StartTimerDto dto, CancellationToken ct = default);
    Task<TimeEntryDto> StopTimerAsync(Guid userId, Guid entryId, StopTimerDto? dto = null, CancellationToken ct = default);
    Task<TimeEntryDto?> GetRunningTimerAsync(Guid userId, CancellationToken ct = default);
    Task<TimeEntryDto> CreateManualEntryAsync(Guid userId, ManualTimeEntryDto dto, CancellationToken ct = default);
    Task DeleteEntryAsync(Guid userId, Guid entryId, CancellationToken ct = default);
    Task<PagedResult<TimeEntryDto>> GetEntriesForTaskAsync(Guid taskId, Guid userId, PageQueryDto query, CancellationToken ct = default);
    Task<List<TimeEntryDto>> GetEntriesForUserAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<TimeReportDto> GetReportAsync(Guid userId, DateTime from, DateTime to, Guid? boardId = null, CancellationToken ct = default);
}
