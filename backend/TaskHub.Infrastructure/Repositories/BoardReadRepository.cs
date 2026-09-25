using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public class BoardReadRepository(AppDbContext db) : IBoardReadRepository
{
    public async Task<ProjectKanbanResponseDto> GetKanbanAsync(Guid projectId, KanbanQueryDto query, CancellationToken ct)
    {
        var boards = db.Boards.AsNoTracking().Where(b => b.ProjectId == projectId);
        if (query.BoardId.HasValue) boards = boards.Where(b => b.Id == query.BoardId);
        var board = await boards.OrderByDescending(b => b.CreatedAt).ThenBy(b => b.Id)
            .Select(b => new BoardSummaryDto { Id=b.Id, Name=b.Name, Color=b.Color, OwnerId=b.OwnerId, ProjectId=b.ProjectId, CreatedAt=b.CreatedAt, UpdatedAt=b.UpdatedAt }).FirstOrDefaultAsync(ct);
        if (board is null && query.BoardId.HasValue) throw new NotFoundException("Board", query.BoardId.Value);
        var result = new ProjectKanbanResponseDto { Board=board, Lists=new() { Page=query.Page, PageSize=query.PageSize } };
        if (board is null) return result;
        result.Lists = await GetColumnsAsync(board.Id, query, ct);
        return result;
    }

    public async Task<PagedResult<KanbanColumnDto>> GetColumnsAsync(Guid boardId, KanbanQueryDto query, CancellationToken ct)
    {
        var result = new ProjectKanbanResponseDto { Lists = new() { Page=query.Page, PageSize=query.PageSize } };
        var columns = db.Lists.AsNoTracking().Where(l => l.BoardId == boardId);
        if (query.ListId.HasValue)
        {
            columns = columns.Where(l => l.Id == query.ListId);
            if (!await columns.AnyAsync(ct)) throw new NotFoundException("BoardList", query.ListId.Value);
        }
        result.Lists.TotalItems = await columns.CountAsync(ct);
        result.Lists.Items = await columns.OrderBy(l => l.Position).ThenBy(l => l.Id)
            .Skip((query.Page-1)*query.PageSize).Take(query.PageSize)
            .Select(l => new KanbanColumnDto { Id=l.Id, BoardId=l.BoardId, Name=l.Name, Color=l.Color, Position=l.Position, CreatedAt=l.CreatedAt, UpdatedAt=l.UpdatedAt }).ToListAsync(ct);
        foreach (var column in result.Lists.Items)
        {
            var tasks = db.Tasks.AsNoTracking().Where(t => t.ListId == column.Id && !t.IsDeleted);
            column.Tasks = new() { Page=query.TaskPage, PageSize=query.TaskPageSize, TotalItems=await tasks.CountAsync(ct) };
            column.Tasks.Items = await tasks.OrderBy(t => t.Position).ThenBy(t => t.Id)
                .Skip((query.TaskPage-1)*query.TaskPageSize).Take(query.TaskPageSize)
                .Select(t => new TaskResponseDto
                {
                    Id=t.Id, Title=t.Title, Description=t.Description, Status=t.Status, Priority=t.Priority,
                    Label=t.Label, Position=t.Position, Progress=t.Progress, DueDate=t.DueDate, StartDate=t.StartDate,
                    ListId=t.ListId, ListName=t.List.Name, BoardId=t.List.BoardId, BoardName=t.List.Board.Name,
                    OwnerId=t.OwnerId, OwnerName=t.Owner.FullName, AssignedToId=t.AssignedToId,
                    AssignedToName=t.AssignedTo == null ? null : t.AssignedTo.FullName,
                    TeamId=t.TeamId, WorkspaceId=t.TeamId ?? (t.List.Board.Project == null ? null : t.List.Board.Project.WorkspaceId),
                    CreatedAt=t.CreatedAt, UpdatedAt=t.UpdatedAt, CommentsCount=t.Comments.Count(c => !c.IsDeleted),
                    AttachmentsCount=t.Attachments.Count,
                    IsOverdue=t.DueDate.HasValue && t.DueDate < DateTime.UtcNow && t.Status != TaskItemStatus.Done
                }).ToListAsync(ct);
        }
        return result.Lists;
    }
}
