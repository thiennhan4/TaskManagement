using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public partial class CollaborationReadRepository(AppDbContext db) : ICollaborationReadRepository
{
    public Task<Board?> BoardAsync(Guid id, CancellationToken ct) =>
        db.Boards.AsNoTracking().Include(b => b.Project).FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<PagedResult<BoardSummaryDto>> BoardsAsync(Guid user, bool admin, Guid? project, PageQueryDto query, CancellationToken ct)
    {
        var boards = db.Boards.AsNoTracking();
        if (project.HasValue) boards = boards.Where(b => b.ProjectId == project);
        else
        {
            var visibleProjects = db.Projects.Where(ResourceVisibility.Projects(user, admin)).Select(p => p.Id);
            boards = boards.Where(b => b.OwnerId == user && (!b.ProjectId.HasValue || visibleProjects.Contains(b.ProjectId.Value)));
        }
        return Page(boards.OrderByDescending(b => b.CreatedAt).ThenBy(b => b.Id).Select(b => new BoardSummaryDto
        { Id=b.Id, Name=b.Name, Color=b.Color, OwnerId=b.OwnerId, ProjectId=b.ProjectId, CreatedAt=b.CreatedAt, UpdatedAt=b.UpdatedAt }), query, ct);
    }

    public Task<PagedResult<CommentResponseDto>> CommentsAsync(Guid task, Guid user, PageQueryDto query, CancellationToken ct) =>
        Page(db.Comments.AsNoTracking().Where(c => c.TaskId == task && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).Select(c => new CommentResponseDto
            { Id=c.Id, Content=c.Content, UserId=c.UserId, UserName=c.User.FullName, UserAvatar=c.User.AvatarUrl,
                CreatedAt=c.CreatedAt, UpdatedAt=c.UpdatedAt, IsOwner=c.UserId == user }), query, ct);

    public Task<PagedResult<ProjectMemberDto>> MembersAsync(Guid project, PageQueryDto query, CancellationToken ct) =>
        Page(db.ProjectMembers.AsNoTracking().Where(m => m.ProjectId == project).OrderBy(m => m.JoinedAt).ThenBy(m => m.Id)
            .Select(m => new ProjectMemberDto { Id=m.Id, ProjectId=m.ProjectId, UserId=m.UserId, FullName=m.User.FullName,
                Email=m.User.Email, AvatarUrl=m.User.AvatarUrl, Role=m.Role, JoinedAt=m.JoinedAt }), query, ct);

    private static async Task<PagedResult<T>> Page<T>(IQueryable<T> source, PageQueryDto query, CancellationToken ct) => new()
    {
        Page=query.Page, PageSize=query.PageSize, TotalItems=await source.CountAsync(ct),
        Items=await source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct)
    };
}
