using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Infrastructure.Repositories;

internal static class ReadProjection
{
    public static readonly Expression<Func<TaskItem, TaskResponseDto>> Task = t => new TaskResponseDto
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
    };

    public static readonly Expression<Func<Project, ProjectResponseDto>> Project = p => new ProjectResponseDto
    {
        Id=p.Id, Name=p.Name, Slug=p.Slug, Description=p.Description, Emoji=p.Emoji, Color=p.Color,
        CoverImageUrl=p.CoverImageUrl, Status=p.Status, Visibility=p.Visibility, ProjectType=p.ProjectType,
        OwnerId=p.OwnerId, OwnerName=p.Owner.FullName, WorkspaceId=p.WorkspaceId, CreatedAt=p.CreatedAt,
        UpdatedAt=p.UpdatedAt, ArchivedAt=p.ArchivedAt, IsArchived=p.ArchivedAt != null,
        MemberCount=p.Members.Count, BoardCount=p.Boards.Count
    };

    public static async Task<PagedResult<T>> Page<T>(IQueryable<T> source, int page, int size, CancellationToken ct) => new()
    {
        Page=page, PageSize=size, TotalItems=await source.CountAsync(ct),
        Items=await source.Skip((page-1)*size).Take(size).ToListAsync(ct)
    };
}
