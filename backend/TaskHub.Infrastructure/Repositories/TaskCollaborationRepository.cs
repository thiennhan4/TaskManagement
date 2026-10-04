using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
namespace TaskHub.Infrastructure.Repositories;
public class TaskCollaborationRepository(AppDbContext db) : ITaskCollaborationRepository
{
    public async Task<int> GetMaxPositionAsync(Guid listId, CancellationToken ct) => await db.Tasks.Where(t=>t.ListId==listId && !t.IsDeleted).MaxAsync(t=>(int?)t.Position,ct) ?? -1;
    public async Task<BoardList> EnsurePersonalListAsync(Guid userId, CancellationToken ct)
    {
        var board=await db.Boards.FirstOrDefaultAsync(b=>b.OwnerId==userId && b.ProjectId==null && b.Name=="Personal Tasks",ct);
        if(board is null) { board=new Board { Name="Personal Tasks", OwnerId=userId }; db.Boards.Add(board); }
        var list=await db.Lists.FirstOrDefaultAsync(l=>l.BoardId==board.Id && l.Name=="Todo",ct);
        if(list is null) { list=new BoardList { Board=board, BoardId=board.Id, Name="Todo", Position=0 }; db.Lists.Add(list); }
        await db.SaveChangesAsync(ct);
        return list;
    }
    public async Task AddAttachmentAsync(TaskAttachment attachment,CancellationToken ct) { db.TaskAttachments.Add(attachment); await db.SaveChangesAsync(ct); }
    public Task<List<TaskAttachment>> GetAttachmentsAsync(Guid taskId,CancellationToken ct) => db.TaskAttachments.Include(a=>a.UploadedByUser).Where(a=>a.TaskId==taskId).OrderByDescending(a=>a.UploadedAt).ToListAsync(ct);
    public Task<TaskAttachment?> GetAttachmentAsync(Guid id,CancellationToken ct) => db.TaskAttachments.Include(a=>a.Task).FirstOrDefaultAsync(a=>a.Id==id,ct);
    public async Task DeleteAttachmentAsync(TaskAttachment attachment,CancellationToken ct) { db.TaskAttachments.Remove(attachment); await db.SaveChangesAsync(ct); }
    public Task<List<TaskActivityLog>> GetActivityAsync(Guid taskId,CancellationToken ct) => db.TaskActivityLogs.Include(l=>l.User).Where(l=>l.TaskId==taskId).OrderByDescending(l=>l.CreatedAt).Take(100).ToListAsync(ct);
    public async Task AddInvitationAsync(TaskInvitation invitation,CancellationToken ct) { db.TaskInvitations.Add(invitation); await db.SaveChangesAsync(ct); }
    public Task<TaskInvitation?> GetInvitationAsync(string token,CancellationToken ct) => db.TaskInvitations.FirstOrDefaultAsync(i=>i.Token==token && !i.IsAccepted,ct);
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
