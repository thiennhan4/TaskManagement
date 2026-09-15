using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Data;

public interface IAppDbContext
{
    DatabaseFacade Database { get; }

    // Phase 1
    DbSet<AppUser> Users { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }
    DbSet<Board> Boards { get; set; }
    DbSet<BoardList> Lists { get; set; }
    DbSet<TaskItem> Tasks { get; set; }
    DbSet<AuditLog> AuditLogs { get; set; }

    // Phase 2
    DbSet<Team> Teams { get; set; }
    DbSet<TeamMember> TeamMembers { get; set; }
    DbSet<Comment> Comments { get; set; }
    DbSet<TaskAttachment> TaskAttachments { get; set; }
    DbSet<TaskActivityLog> TaskActivityLogs { get; set; }
    DbSet<NotificationType> NotificationTypes { get; set; }
    DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
    DbSet<EmailTrackingToken> EmailTrackingTokens { get; set; }
    DbSet<TaskInvitation> TaskInvitations { get; set; }

    // Phase 3
    DbSet<Project> Projects { get; set; }
    DbSet<ProjectMember> ProjectMembers { get; set; }
    DbSet<ProjectInvitation> ProjectInvitations { get; set; }
    DbSet<ProjectActivityLog> ProjectActivityLogs { get; set; }

    // Phase 5: Productivity
    DbSet<TimeEntry> TimeEntries { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
