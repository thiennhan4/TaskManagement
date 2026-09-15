using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Data;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services;

public class TimeTrackingService : ITimeTrackingService
{
    private readonly IAppDbContext _context;

    public TimeTrackingService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<TimeEntryDto> StartTimerAsync(Guid userId, StartTimerDto dto)
    {
        // Prevent multiple running timers
        var running = await _context.TimeEntries
            .FirstOrDefaultAsync(te => te.UserId == userId && te.EndTime == null);

        if (running != null)
            throw new InvalidOperationException("You already have a running timer. Stop it before starting a new one.");

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId && !t.IsDeleted)
            ?? throw new KeyNotFoundException("Task not found.");

        var entry = new TimeEntry
        {
            TaskId = dto.TaskId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
            Description = dto.Description,
            IsBillable = dto.IsBillable
        };

        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();

        return MapToDto(entry, task.Title, null);
    }

    public async Task<TimeEntryDto> StopTimerAsync(Guid userId, Guid entryId, StopTimerDto? dto = null)
    {
        var entry = await _context.TimeEntries
            .Include(te => te.Task)
            .FirstOrDefaultAsync(te => te.Id == entryId && te.UserId == userId)
            ?? throw new KeyNotFoundException("Time entry not found.");

        if (entry.EndTime != null)
            throw new InvalidOperationException("Timer is already stopped.");

        entry.EndTime = DateTime.UtcNow;
        entry.DurationSeconds = (int)(entry.EndTime.Value - entry.StartTime).TotalSeconds;
        entry.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto?.Description))
            entry.Description = dto.Description;

        await _context.SaveChangesAsync();

        return MapToDto(entry, entry.Task.Title, null);
    }

    public async Task<TimeEntryDto?> GetRunningTimerAsync(Guid userId)
    {
        var entry = await _context.TimeEntries
            .Include(te => te.Task)
            .FirstOrDefaultAsync(te => te.UserId == userId && te.EndTime == null);

        if (entry == null) return null;

        return MapToDto(entry, entry.Task.Title, null);
    }

    public async Task<TimeEntryDto> CreateManualEntryAsync(Guid userId, ManualTimeEntryDto dto)
    {
        if (dto.EndTime <= dto.StartTime)
            throw new ArgumentException("End time must be after start time.");

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId && !t.IsDeleted)
            ?? throw new KeyNotFoundException("Task not found.");

        var entry = new TimeEntry
        {
            TaskId = dto.TaskId,
            UserId = userId,
            StartTime = dto.StartTime.ToUniversalTime(),
            EndTime = dto.EndTime.ToUniversalTime(),
            DurationSeconds = (int)(dto.EndTime - dto.StartTime).TotalSeconds,
            Description = dto.Description,
            IsBillable = dto.IsBillable
        };

        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();

        return MapToDto(entry, task.Title, null);
    }

    public async Task DeleteEntryAsync(Guid userId, Guid entryId)
    {
        var entry = await _context.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == entryId && te.UserId == userId)
            ?? throw new KeyNotFoundException("Time entry not found.");

        _context.TimeEntries.Remove(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TimeEntryDto>> GetEntriesForTaskAsync(Guid taskId)
    {
        return await _context.TimeEntries
            .AsNoTracking()
            .Where(te => te.TaskId == taskId)
            .Include(te => te.User)
            .Include(te => te.Task)
            .OrderByDescending(te => te.StartTime)
            .Select(te => new TimeEntryDto
            {
                Id = te.Id,
                TaskId = te.TaskId,
                TaskTitle = te.Task.Title,
                UserId = te.UserId,
                UserName = te.User.FullName,
                StartTime = te.StartTime,
                EndTime = te.EndTime,
                DurationSeconds = te.EndTime == null
                    ? (int)(DateTime.UtcNow - te.StartTime).TotalSeconds
                    : te.DurationSeconds,
                Description = te.Description,
                IsBillable = te.IsBillable,
                CreatedAt = te.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<TimeEntryDto>> GetEntriesForUserAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.TimeEntries
            .AsNoTracking()
            .Where(te => te.UserId == userId);

        if (from.HasValue)
            query = query.Where(te => te.StartTime >= from.Value.ToUniversalTime());
        if (to.HasValue)
            query = query.Where(te => te.StartTime <= to.Value.ToUniversalTime());

        return await query
            .Include(te => te.Task)
            .OrderByDescending(te => te.StartTime)
            .Take(100)
            .Select(te => new TimeEntryDto
            {
                Id = te.Id,
                TaskId = te.TaskId,
                TaskTitle = te.Task.Title,
                UserId = te.UserId,
                StartTime = te.StartTime,
                EndTime = te.EndTime,
                DurationSeconds = te.EndTime == null
                    ? (int)(DateTime.UtcNow - te.StartTime).TotalSeconds
                    : te.DurationSeconds,
                Description = te.Description,
                IsBillable = te.IsBillable,
                CreatedAt = te.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TimeReportDto> GetReportAsync(Guid userId, DateTime from, DateTime to, Guid? boardId = null)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        var query = _context.TimeEntries
            .AsNoTracking()
            .Include(te => te.Task)
            .Include(te => te.User)
            .Where(te => te.UserId == userId && te.EndTime != null && te.StartTime >= fromUtc && te.StartTime <= toUtc);

        if (boardId.HasValue)
        {
            query = query.Where(te => te.Task.List.BoardId == boardId.Value);
        }

        var entries = await query.ToListAsync();

        var report = new TimeReportDto
        {
            TotalSeconds = entries.Sum(e => e.DurationSeconds),
            BillableSeconds = entries.Where(e => e.IsBillable).Sum(e => e.DurationSeconds),
            NonBillableSeconds = entries.Where(e => !e.IsBillable).Sum(e => e.DurationSeconds),
            EntryCount = entries.Count,
            ByTask = entries.GroupBy(e => e.TaskId).Select(g => new TimeReportGroupDto
            {
                Id = g.Key.ToString(),
                Name = g.First().Task.Title,
                TotalSeconds = g.Sum(e => e.DurationSeconds),
                EntryCount = g.Count()
            }).OrderByDescending(x => x.TotalSeconds).ToList(),
            ByUser = entries.GroupBy(e => e.UserId).Select(g => new TimeReportGroupDto
            {
                Id = g.Key.ToString(),
                Name = g.First().User.FullName ?? "Unknown",
                TotalSeconds = g.Sum(e => e.DurationSeconds),
                EntryCount = g.Count()
            }).OrderByDescending(x => x.TotalSeconds).ToList(),
            ByDay = entries.GroupBy(e => e.StartTime.Date).Select(g => new TimeReportDayDto
            {
                Date = g.Key,
                TotalSeconds = g.Sum(e => e.DurationSeconds),
                EntryCount = g.Count()
            }).OrderBy(x => x.Date).ToList()
        };

        return report;
    }

    private static TimeEntryDto MapToDto(TimeEntry entry, string? taskTitle, string? userName)
    {
        return new TimeEntryDto
        {
            Id = entry.Id,
            TaskId = entry.TaskId,
            TaskTitle = taskTitle,
            UserId = entry.UserId,
            UserName = userName,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            DurationSeconds = entry.EndTime == null
                ? (int)(DateTime.UtcNow - entry.StartTime).TotalSeconds
                : entry.DurationSeconds,
            Description = entry.Description,
            IsBillable = entry.IsBillable,
            CreatedAt = entry.CreatedAt
        };
    }
}
