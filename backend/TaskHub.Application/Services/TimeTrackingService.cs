using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Application.Validators;
namespace TaskHub.Application.Services;

public class TimeTrackingService(ITimeTrackingRepository repository, ITaskItemRepository tasks, IPermissionService permissions) : ITimeTrackingService
{
    private async Task<TaskItem> Authorize(Guid taskId, Guid userId, TaskAction action)
    {
        var task = await tasks.GetTaskForAuthorizationAsync(taskId) ?? throw new NotFoundException("Task", taskId);
        await permissions.AuthorizeTaskActionAsync(userId, task, action);
        return task;
    }
    public async Task<TimeEntryDto> StartTimerAsync(Guid userId, StartTimerDto dto)
    {
        var task = await Authorize(dto.TaskId, userId, TaskAction.Update);
        if (await repository.GetRunningAsync(userId) is not null) throw new ConflictException("Stop your running timer first.");
        var entry = new TimeEntry { TaskId=dto.TaskId, UserId=userId, StartTime=DateTime.UtcNow, Description=dto.Description, IsBillable=dto.IsBillable };
        await repository.AddAsync(entry);
        return MapToDto(entry, task.Title, null);
    }
    public async Task<TimeEntryDto> StopTimerAsync(Guid userId, Guid entryId, StopTimerDto? dto = null)
    {
        var entry = await repository.GetAsync(entryId) ?? throw new NotFoundException("TimeEntry", entryId);
        await permissions.AuthorizeEntryOwnerAsync(userId, entry.UserId);
        var task = await Authorize(entry.TaskId, userId, TaskAction.View);
        if (entry.EndTime.HasValue) throw new ConflictException("Timer is already stopped.");
        entry.EndTime=DateTime.UtcNow;
        entry.DurationSeconds=(int)(entry.EndTime.Value-entry.StartTime).TotalSeconds;
        entry.UpdatedAt=DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto?.Description)) entry.Description=dto.Description;
        await repository.SaveAsync();
        return MapToDto(entry, task.Title, null);
    }
    public async Task<TimeEntryDto?> GetRunningTimerAsync(Guid userId)
    {
        var entry=await repository.GetRunningAsync(userId);
        if(entry is null) return null;
        var task=await Authorize(entry.TaskId,userId,TaskAction.View);
        return MapToDto(entry,task.Title,null);
    }
    public async Task<TimeEntryDto> CreateManualEntryAsync(Guid userId, ManualTimeEntryDto dto)
    {
        if (!new ManualTimeEntryValidator().Validate(dto).IsValid) throw new BadRequestException("Invalid time range.");
        var task=await Authorize(dto.TaskId,userId,TaskAction.Update);
        var entry=new TimeEntry { TaskId=dto.TaskId, UserId=userId, StartTime=dto.StartTime.ToUniversalTime(), EndTime=dto.EndTime.ToUniversalTime(), DurationSeconds=(int)(dto.EndTime-dto.StartTime).TotalSeconds, Description=dto.Description, IsBillable=dto.IsBillable };
        await repository.AddAsync(entry);
        return MapToDto(entry,task.Title,null);
    }
    public async Task DeleteEntryAsync(Guid userId, Guid entryId)
    {
        var entry=await repository.GetAsync(entryId) ?? throw new NotFoundException("TimeEntry",entryId);
        await permissions.AuthorizeEntryOwnerAsync(userId,entry.UserId);
        await Authorize(entry.TaskId,userId,TaskAction.View);
        await repository.DeleteAsync(entry);
    }
    public async Task<PagedResult<TimeEntryDto>> GetEntriesForTaskAsync(Guid taskId, Guid userId, PageQueryDto query)
    {
        await Authorize(taskId,userId,TaskAction.View);
        return await repository.GetTaskPageAsync(taskId,query);
    }
    public async Task<List<TimeEntryDto>> GetEntriesForUserAsync(Guid userId, DateTime? from=null, DateTime? to=null)
    {
        var entries=await repository.GetUserEntriesAsync(userId,from,to,null);
        var result=new List<TimeEntryDto>();
        foreach(var entry in entries)
        {
            try { await Authorize(entry.TaskId,userId,TaskAction.View); } catch (ForbiddenException) { continue; } catch (NotFoundException) { continue; }
            result.Add(MapToDto(entry,entry.Task.Title,null));
        }
        return result;
    }
    public async Task<TimeReportDto> GetReportAsync(Guid userId, DateTime from, DateTime to, Guid? boardId=null)
    {
        if(to < from) throw new BadRequestException("Invalid report range.");
        var candidates=await repository.GetUserEntriesAsync(userId,from,to,boardId);
        var entries=new List<TimeEntry>();
        foreach(var entry in candidates.Where(e=>e.EndTime.HasValue))
        {
            try { await Authorize(entry.TaskId,userId,TaskAction.View); } catch (ForbiddenException) { continue; } catch (NotFoundException) { continue; }
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
