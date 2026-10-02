using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services;
using TaskHub.Domain.Entities;

namespace TaskHub.Infrastructure.Repositories;

public partial class TaskItemRepository
{
    private IQueryable<TaskItem> VisibleTasks(Guid user, bool admin) => _context.Tasks.AsNoTracking()
        .Where(t => !t.IsDeleted && (t.List.Board.Project == null || t.List.Board.Project.ArchivedAt == null))
        .Where(ResourceVisibility.Tasks(user, admin));

    private IQueryable<TaskItem> Filter(TaskFilterDto filter, Guid user, bool admin)
    {
        var q = VisibleTasks(user, admin);
        if (filter.Status.HasValue) q=q.Where(t=>t.Status==filter.Status);
        if (filter.Priority.HasValue) q=q.Where(t=>t.Priority==filter.Priority);
        if (filter.BoardId.HasValue) q=q.Where(t=>t.List.BoardId==filter.BoardId);
        if (filter.ListId.HasValue) q=q.Where(t=>t.ListId==filter.ListId);
        if (filter.AssignedToUserId.HasValue) q=q.Where(t=>t.AssignedToId==filter.AssignedToUserId);
        if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
        {
            var keyword=filter.SearchKeyword.Trim();
            q=q.Where(t=>t.Title.Contains(keyword) || (t.Description!=null && t.Description.Contains(keyword)));
        }
        if (filter.IsOverdue==true) q=q.Where(t=>t.DueDate<DateTime.UtcNow && t.Status!=TaskItemStatus.Done);
        return q;
    }

    private static IOrderedQueryable<TaskItem> Order(IQueryable<TaskItem> q, TaskFilterDto f)
    {
        var desc=string.Equals(f.SortOrder,"desc",StringComparison.OrdinalIgnoreCase);
        return (f.SortBy?.ToLowerInvariant() switch
        {
            "duedate" => desc?q.OrderByDescending(t=>t.DueDate):q.OrderBy(t=>t.DueDate),
            "priority" => desc?q.OrderByDescending(t=>t.Priority):q.OrderBy(t=>t.Priority),
            "title" => desc?q.OrderByDescending(t=>t.Title):q.OrderBy(t=>t.Title),
            _ => string.Equals(f.SortOrder,"asc",StringComparison.OrdinalIgnoreCase)?q.OrderBy(t=>t.CreatedAt):q.OrderByDescending(t=>t.CreatedAt)
        }).ThenBy(t=>t.Id);
    }

    public Task<PagedResult<TaskResponseDto>> GetPageAsync(TaskFilterDto filter, Guid user, bool admin, CancellationToken ct = default) =>
        ReadProjection.Page(Order(Filter(filter,user,admin),filter).Select(ReadProjection.Task),filter.Page,filter.PageSize,ct);

    public async Task<TaskSummaryDto> GetSummaryAsync(Guid user, bool admin, CancellationToken ct = default) =>
        await VisibleTasks(user,admin).GroupBy(t=>1).Select(g=>new TaskSummaryDto
        {
            Total=g.Count(), Todo=g.Count(t=>t.Status==TaskItemStatus.Todo), InProgress=g.Count(t=>t.Status==TaskItemStatus.InProgress),
            Done=g.Count(t=>t.Status==TaskItemStatus.Done), Review=g.Count(t=>t.Status==TaskItemStatus.Review)
        }).SingleOrDefaultAsync(ct) ?? new();

    public Task<TaskResponseDto?> GetHeaderAsync(Guid id, CancellationToken ct = default) =>
        _context.Tasks.AsNoTracking().Where(t=>t.Id==id && !t.IsDeleted).Select(ReadProjection.Task).SingleOrDefaultAsync(ct);

    public Task<PagedResult<TaskCalendarDto>> GetCalendarPageAsync(CalendarFilterDto filter, Guid user, CancellationToken ct = default)
    {
        var q=VisibleTasks(user,false).Where(t => (t.DueDate>=filter.Start && t.DueDate<=filter.End) ||
            (t.StartDate>=filter.Start && t.StartDate<=filter.End) || (t.StartDate<=filter.Start && t.DueDate>=filter.End));
        if (filter.ProjectId.HasValue) q=q.Where(t=>t.List.Board.ProjectId==filter.ProjectId);
        if (filter.BoardId.HasValue) q=q.Where(t=>t.List.BoardId==filter.BoardId);
        return ReadProjection.Page(q.OrderBy(t=>t.StartDate ?? t.DueDate).ThenBy(t=>t.Id).Select(t=>new TaskCalendarDto
        {
            Id=t.Id, Title=t.Title, StartDate=t.StartDate, DueDate=t.DueDate, Status=t.Status, Priority=t.Priority,
            BoardName=t.List.Board.Name, ProjectName=t.List.Board.Project==null?null:t.List.Board.Project.Name,
            Color=t.List.Board.Project==null?t.List.Board.Color:t.List.Board.Project.Color ?? t.List.Board.Color
        }),filter.Page,filter.PageSize,ct);
    }
}
