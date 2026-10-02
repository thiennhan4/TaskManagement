using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Application.Validators;
namespace TaskHub.Application.Services;

public class TimeTrackingService(ITimeTrackingRepository repository, ITaskItemRepository tasks, IPermissionService permissions, IMutationRunner mutations) : ITimeTrackingService
{
    private readonly IMutationRunner _mutations = mutations;
    private async Task<TaskItem> Authorize(Guid taskId, Guid userId, TaskAction action, CancellationToken ct = default)
    {
        var task = await tasks.GetTaskForAuthorizationAsync(taskId, ct: ct) ?? throw new NotFoundException("Task", taskId);
        await permissions.AuthorizeTaskActionAsync(userId, task, action, ct: ct);
        return task;
    }
    public Task<TimeEntryDto> StartTimerAsync(Guid userId, StartTimerDto dto, CancellationToken ct = default) =>
        _mutations.RunAsync(() => StartTimerAsyncCore(userId, dto, ct), ct);

    private async Task<TimeEntryDto> StartTimerAsyncCore(Guid userId, StartTimerDto dto, CancellationToken ct = default)
    {
        var task = await Authorize(dto.TaskId, userId, TaskAction.Update, ct: ct);
        if (await repository.GetRunningAsync(userId, ct: ct) is not null) throw new ConflictException("Stop your running timer first.");
        var entry = new TimeEntry { TaskId=dto.TaskId, UserId=userId, StartTime=DateTime.UtcNow, Description=dto.Description, IsBillable=dto.IsBillable };
        await repository.AddAsync(entry, ct: ct);
        return MapToDto(entry, task.Title, null);
    }
    public Task<TimeEntryDto> StopTimerAsync(Guid userId, Guid entryId, StopTimerDto? dto = null, CancellationToken ct = default) =>
        _mutations.RunAsync(() => StopTimerAsyncCore(userId, entryId, dto, ct), ct);

    private async Task<TimeEntryDto> StopTimerAsyncCore(Guid userId, Guid entryId, StopTimerDto? dto = null, CancellationToken ct = default)
    {
        var entry = await repository.GetAsync(entryId, ct: ct) ?? throw new NotFoundException("TimeEntry", entryId);
        await permissions.AuthorizeEntryOwnerAsync(userId, entry.UserId, ct: ct);
        var task = await Authorize(entry.TaskId, userId, TaskAction.View, ct: ct);
        if (entry.EndTime.HasValue) throw new ConflictException("Timer is already stopped.");
        entry.EndTime=DateTime.UtcNow;
        entry.DurationSeconds=(int)(entry.EndTime.Value-entry.StartTime).TotalSeconds;
        entry.UpdatedAt=DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto?.Description)) entry.Description=dto.Description;
        await repository.SaveAsync(ct: ct);
        return MapToDto(entry, task.Title, null);
    }
    public async Task<TimeEntryDto?> GetRunningTimerAsync(Guid userId, CancellationToken ct = default)
    {
        var entry=await repository.GetRunningAsync(userId, ct: ct);
        if(entry is null) return null;
        var task=await Authorize(entry.TaskId,userId,TaskAction.View, ct: ct);
        return MapToDto(entry,task.Title,null);
    }
    public Task<TimeEntryDto> CreateManualEntryAsync(Guid userId, ManualTimeEntryDto dto, CancellationToken ct = default) =>
        _mutations.RunAsync(() => CreateManualEntryAsyncCore(userId, dto, ct), ct);

    private async Task<TimeEntryDto> CreateManualEntryAsyncCore(Guid userId, ManualTimeEntryDto dto, CancellationToken ct = default)
    {
        if (!new ManualTimeEntryValidator().Validate(dto).IsValid) throw new BadRequestException("Invalid time range.");
        var task=await Authorize(dto.TaskId,userId,TaskAction.Update, ct: ct);
        var entry=new TimeEntry { TaskId=dto.TaskId, UserId=userId, StartTime=dto.StartTime.ToUniversalTime(), EndTime=dto.EndTime.ToUniversalTime(), DurationSeconds=(int)(dto.EndTime-dto.StartTime).TotalSeconds, Description=dto.Description, IsBillable=dto.IsBillable };
        await repository.AddAsync(entry, ct: ct);
        return MapToDto(entry,task.Title,null);
    }
    public Task DeleteEntryAsync(Guid userId, Guid entryId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => DeleteEntryAsyncCore(userId, entryId, ct), ct);

    private async Task DeleteEntryAsyncCore(Guid userId, Guid entryId, CancellationToken ct = default)
    {
        var entry=await repository.GetAsync(entryId, ct: ct) ?? throw new NotFoundException("TimeEntry",entryId);
        await permissions.AuthorizeEntryOwnerAsync(userId,entry.UserId, ct: ct);
        await Authorize(entry.TaskId,userId,TaskAction.View, ct: ct);
        await repository.DeleteAsync(entry, ct: ct);
    }
    public async Task<PagedResult<TimeEntryDto>> GetEntriesForTaskAsync(Guid taskId, Guid userId, PageQueryDto query, CancellationToken ct = default)
    {
        await Authorize(taskId,userId,TaskAction.View, ct: ct);
        return await repository.GetTaskPageAsync(taskId,query, ct: ct);
    }
    public async Task<List<TimeEntryDto>> GetEntriesForUserAsync(Guid userId, DateTime? from=null, DateTime? to=null, CancellationToken ct = default)
    {
        var entries=await repository.GetUserEntriesAsync(userId,from,to,null, ct: ct);
        var result=new List<TimeEntryDto>();
        foreach(var entry in entries)
        {
            try { await Authorize(entry.TaskId,userId,TaskAction.View, ct: ct); } catch (ForbiddenException) { continue; } catch (NotFoundException) { continue; }
            result.Add(MapToDto(entry,entry.Task.Title,null));
        }
        return result;
    }
    public async Task<TimeReportDto> GetReportAsync(Guid userId, DateTime from, DateTime to, Guid? boardId=null, CancellationToken ct = default)
    {
        if(to < from) throw new BadRequestException("Invalid report range.");
        var candidates=await repository.GetUserEntriesAsync(userId,from,to,boardId, ct: ct);
        var entries=new List<TimeEntry>();
        foreach(var entry in candidates.Where(e=>e.EndTime.HasValue))
        {
            try { await Authorize(entry.TaskId,userId,TaskAction.View, ct: ct); } catch (ForbiddenException) { continue; } catch (NotFoundException) { continue; }
            entries.Add(entry);
        }
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
