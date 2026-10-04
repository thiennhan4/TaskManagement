using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services;
using TaskHub.Domain.Entities;

namespace TaskHub.Infrastructure.Repositories;

public partial class CollaborationReadRepository
{
    public Task<TeamResponseDto?> TeamAsync(Guid id, Guid user, CancellationToken ct) =>
        db.Teams.AsNoTracking().Where(t => t.Id == id).Select(t => new TeamResponseDto {
            Id=t.Id, Name=t.Name, Description=t.Description, CreatedById=t.CreatedById,
            CreatedByName=t.CreatedBy.FullName, CreatedAt=t.CreatedAt, MemberCount=t.Members.Count,
            CurrentUserRole=t.Members.Where(m => m.UserId == user).Select(m => (TeamRole?)m.Role).FirstOrDefault()
        }).SingleOrDefaultAsync(ct);
    public Task<PagedResult<ProjectResponseDto>> ProjectsAsync(Guid user, bool admin, ProjectQueryDto query, CancellationToken ct)
    {
        var q=db.Projects.AsNoTracking().Where(ResourceVisibility.Projects(user,admin));
        if(query.WorkspaceId.HasValue) q=q.Where(p=>p.WorkspaceId==query.WorkspaceId);
        if(query.ArchivedOnly) q=q.Where(p=>p.ArchivedAt!=null);
        else if(!query.IncludeArchived) q=q.Where(p=>p.ArchivedAt==null);
        if(query.Status.HasValue) q=q.Where(p=>p.Status==query.Status);
        if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(p=>p.Name.Contains(query.Search));
        return ReadProjection.Page(q.OrderByDescending(p=>p.CreatedAt).ThenBy(p=>p.Id).Select(ReadProjection.Project),query.Page,query.PageSize,ct);
    }
    public Task<ProjectResponseDto?> ProjectAsync(Guid id, CancellationToken ct) =>
        db.Projects.AsNoTracking().Where(p=>p.Id==id).Select(ReadProjection.Project).SingleOrDefaultAsync(ct);

    public Task<PagedResult<TeamResponseDto>> TeamsAsync(Guid user, PageQueryDto query, CancellationToken ct) =>
        ReadProjection.Page(db.Teams.AsNoTracking().Where(t=>t.Members.Any(m=>m.UserId==user))
            .OrderByDescending(t=>t.CreatedAt).ThenBy(t=>t.Id).Select(t=>new TeamResponseDto
            { Id=t.Id, Name=t.Name, Description=t.Description, CreatedById=t.CreatedById, CreatedByName=t.CreatedBy.FullName,
                CreatedAt=t.CreatedAt, MemberCount=t.Members.Count, CurrentUserRole=t.Members.Where(m=>m.UserId==user).Select(m=>(TeamRole?)m.Role).FirstOrDefault() }),query.Page,query.PageSize,ct);

    public Task<PagedResult<TeamMemberResponseDto>> TeamMembersAsync(Guid team, PageQueryDto query, CancellationToken ct) =>
        ReadProjection.Page(db.TeamMembers.AsNoTracking().Where(m=>m.TeamId==team).OrderBy(m=>m.JoinedAt).ThenBy(m=>m.Id)
            .Select(m=>new TeamMemberResponseDto { Id=m.Id, UserId=m.UserId, FullName=m.User.FullName, Email=m.User.Email,
                AvatarUrl=m.User.AvatarUrl, Role=m.Role, JoinedAt=m.JoinedAt }),query.Page,query.PageSize,ct);

    public Task<PagedResult<AttachmentResponseDto>> AttachmentsAsync(Guid task, Guid user, bool admin, PageQueryDto query, CancellationToken ct) =>
        ReadProjection.Page(db.TaskAttachments.AsNoTracking().Where(a=>a.TaskId==task).OrderByDescending(a=>a.UploadedAt).ThenBy(a=>a.Id)
            .Select(a=>new AttachmentResponseDto { Id=a.Id, FileName=a.FileName, FileSize=a.FileSize, ContentType=a.ContentType,
                UploadedAt=a.UploadedAt, UploadedByUserName=a.UploadedByUser.FullName, FilePath="",
                FileUrl="/api/v1/tasks/"+task+"/attachments/"+a.Id+"/download", CanDelete=admin || a.UploadedByUserId==user || a.Task.OwnerId==user }),query.Page,query.PageSize,ct);

    public async Task<PagedResult<ActivityLogResponseDto>> TaskActivityAsync(Guid task, PageQueryDto query, CancellationToken ct)
    {
        var q=db.TaskActivityLogs.AsNoTracking().Where(l=>l.TaskId==task);
        var total=await q.CountAsync(ct);
        var rows=await q.OrderByDescending(l=>l.CreatedAt).ThenBy(l=>l.Id).Skip((query.Page-1)*query.PageSize).Take(query.PageSize)
            .Select(l=>new { l.Id,l.Action,l.OldValue,l.NewValue,l.CreatedAt,UserName=l.User.FullName }).ToListAsync(ct);
        return new() { Page=query.Page,PageSize=query.PageSize,TotalItems=total,Items=rows.Select(l=>new ActivityLogResponseDto
            { Id=l.Id,Action=l.Action.ToString(),ActionDescription=l.Action.ToString(),OldValue=l.OldValue,NewValue=l.NewValue,UserName=l.UserName,CreatedAt=l.CreatedAt }).ToList() };
    }

    public Task<PagedResult<ProjectActivityResponseDto>> ProjectActivityAsync(Guid project, PageQueryDto query, CancellationToken ct) =>
        ReadProjection.Page(db.ProjectActivityLogs.AsNoTracking().Where(l=>l.ProjectId==project).OrderByDescending(l=>l.CreatedAt).ThenBy(l=>l.Id)
            .Select(l=>new ProjectActivityResponseDto { Id=l.Id,ProjectId=l.ProjectId,UserId=l.UserId,Action=l.Action,Description=l.Description,
                CreatedAt=l.CreatedAt,User=new UserSummaryDto { Id=l.UserId,FullName=l.User.FullName,AvatarUrl=l.User.AvatarUrl } }),query.Page,query.PageSize,ct);
}
