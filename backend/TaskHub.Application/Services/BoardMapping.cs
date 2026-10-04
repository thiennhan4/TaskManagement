using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
namespace TaskHub.Application.Services;

public static class BoardMapping
{
    public static BoardSummaryDto Summary(Board b) => new() { Id=b.Id, Name=b.Name, Color=b.Color, OwnerId=b.OwnerId, ProjectId=b.ProjectId, CreatedAt=b.CreatedAt, UpdatedAt=b.UpdatedAt };
    public static BoardResponseDto Detail(Board b) => new() { Id=b.Id, Name=b.Name, Color=b.Color, OwnerId=b.OwnerId, ProjectId=b.ProjectId, CreatedAt=b.CreatedAt, UpdatedAt=b.UpdatedAt, Lists=b.Lists.OrderBy(l=>l.Position).ThenBy(l=>l.Id).Select(Column).ToList() };
    public static BoardListResponseDto Column(BoardList l) => new() { Id=l.Id, Name=l.Name, BoardId=l.BoardId, Color=l.Color, Position=l.Position, CreatedAt=l.CreatedAt, UpdatedAt=l.UpdatedAt, Tasks=l.Tasks.Where(t=>!t.IsDeleted).OrderBy(t=>t.Position).ThenBy(t=>t.Id).Select(Task).ToList() };
    public static TaskResponseDto Task(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        Label = task.Label,
        Position = task.Position,
        Progress = task.Progress,
        DueDate = task.DueDate,
        StartDate = task.StartDate,
        ListId = task.ListId,
        ListName = task.List?.Name,
        BoardId = task.List?.BoardId,
        BoardName = task.List?.Board?.Name,
        OwnerId = task.OwnerId,
        OwnerName = task.Owner?.FullName,
        AssignedToId = task.AssignedToId,
        AssignedToName = task.AssignedTo?.FullName,
        TeamId = task.TeamId,
        WorkspaceId = task.TeamId ?? task.List?.Board?.Project?.WorkspaceId,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        CommentsCount = task.Comments?.Count(c => !c.IsDeleted) ?? 0,
        AttachmentsCount = task.Attachments?.Count ?? 0,
        IsOverdue = task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow && task.Status != TaskItemStatus.Done
    };


}
