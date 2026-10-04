using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services;
using TaskHub.Domain.Entities;

namespace TaskHub.Infrastructure.Repositories;

public partial class TimeTrackingRepository
{
    private IQueryable<TimeEntry> VisibleEntries(Guid user, bool admin, TimeQueryDto query)
    {
        var tasks = db.Tasks.Where(t => !t.IsDeleted).Where(ResourceVisibility.Tasks(user, admin));
        var entries = db.TimeEntries.AsNoTracking().Where(e => e.UserId == user && tasks.Any(t => t.Id == e.TaskId)
            && e.StartTime >= query.From && e.StartTime <= query.To);
        if (query.BoardId.HasValue) entries = entries.Where(e => e.Task.List.BoardId == query.BoardId);
        return entries;
    }

    public async Task<PagedResult<TimeEntryDto>> GetUserPageAsync(Guid user, bool admin, TimeQueryDto query, CancellationToken ct)
    {
        var result = await ReadProjection.Page(VisibleEntries(user, admin, query).OrderByDescending(e => e.StartTime).ThenBy(e => e.Id)
            .Select(e => new TimeEntryDto { Id=e.Id, TaskId=e.TaskId, TaskTitle=e.Task.Title, UserId=e.UserId, UserName=e.User.FullName,
                StartTime=e.StartTime, EndTime=e.EndTime, DurationSeconds=e.DurationSeconds, Description=e.Description,
                IsBillable=e.IsBillable, CreatedAt=e.CreatedAt }), query.Page, query.PageSize, ct);
        ApplyRunningDurations(result.Items);
        return result;
    }

    private static void ApplyRunningDurations(IEnumerable<TimeEntryDto> entries)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in entries.Where(e => !e.EndTime.HasValue))
            entry.DurationSeconds = (int)Math.Clamp((now - entry.StartTime).TotalSeconds, 0, int.MaxValue);
    }

    public async Task<TimeReportDto> GetReportAsync(Guid user, bool admin, TimeQueryDto query, CancellationToken ct)
    {
        // Reports retain the existing stopped-entry policy. Live durations are only in history/running responses.
        var entries = VisibleEntries(user, admin, query).Where(e => e.EndTime.HasValue);
        var result = await entries.GroupBy(e => 1).Select(g => new TimeReportDto {
            TotalSeconds=g.Sum(e => e.DurationSeconds), BillableSeconds=g.Sum(e => e.IsBillable ? e.DurationSeconds : 0),
            NonBillableSeconds=g.Sum(e => e.IsBillable ? 0 : e.DurationSeconds), EntryCount=g.Count()
        }).SingleOrDefaultAsync(ct) ?? new();
        var groups = entries.GroupBy(e => new { e.TaskId, e.Task.Title }).Select(g => new {
            g.Key.TaskId, g.Key.Title, TotalSeconds=g.Sum(e => e.DurationSeconds), EntryCount=g.Count()
        });
        result.TotalTaskGroups = await groups.CountAsync(ct);
        result.TaskGroupPage = query.Page;
        result.TaskGroupPageSize = query.PageSize;
        var tasks = await groups.OrderByDescending(g => g.TotalSeconds).ThenBy(g => g.TaskId)
            .Skip((query.Page-1)*query.PageSize).Take(query.PageSize).ToListAsync(ct);
        result.ByTask = tasks.Select(g => new TimeReportGroupDto { Id=g.TaskId.ToString(), Name=g.Title, TotalSeconds=g.TotalSeconds, EntryCount=g.EntryCount }).ToList();
        result.ByUser = await entries.GroupBy(e => new { e.UserId, e.User.FullName }).Select(g => new TimeReportGroupDto {
            Id=g.Key.UserId.ToString(), Name=g.Key.FullName, TotalSeconds=g.Sum(e => e.DurationSeconds), EntryCount=g.Count()
        }).ToListAsync(ct);
        result.ByDay = await entries.GroupBy(e => e.StartTime.Date).OrderBy(g => g.Key).Select(g => new TimeReportDayDto {
            Date=g.Key, TotalSeconds=g.Sum(e => e.DurationSeconds), EntryCount=g.Count()
        }).ToListAsync(ct);
        return result;
    }
}
