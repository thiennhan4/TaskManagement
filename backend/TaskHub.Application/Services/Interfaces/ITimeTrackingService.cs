using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

public interface ITimeTrackingService
{
    Task<TimeEntryDto> StartTimerAsync(Guid userId, StartTimerDto dto);
    Task<TimeEntryDto> StopTimerAsync(Guid userId, Guid entryId, StopTimerDto? dto = null);
    Task<TimeEntryDto?> GetRunningTimerAsync(Guid userId);
    Task<TimeEntryDto> CreateManualEntryAsync(Guid userId, ManualTimeEntryDto dto);
    Task DeleteEntryAsync(Guid userId, Guid entryId);
    Task<List<TimeEntryDto>> GetEntriesForTaskAsync(Guid taskId);
    Task<List<TimeEntryDto>> GetEntriesForUserAsync(Guid userId, DateTime? from = null, DateTime? to = null);
    Task<TimeReportDto> GetReportAsync(Guid userId, DateTime from, DateTime to, Guid? boardId = null);
}
