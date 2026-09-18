using Microsoft.EntityFrameworkCore;
using TaskHub.Domain.Entities;

namespace TaskHub.Infrastructure.Data;

public class AppDbContext : DbContext, TaskHub.Application.Data.IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // â”€â”€ Phase 1 â”€â”€
    public DbSet<AppUser> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Board> Boards { get; set; }
    public DbSet<BoardList> Lists { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    // â”€â”€ Phase 2 â”€â”€
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<TaskAttachment> TaskAttachments { get; set; }
    public DbSet<TaskActivityLog> TaskActivityLogs { get; set; }
    public DbSet<NotificationType> NotificationTypes { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
    public DbSet<EmailTrackingToken> EmailTrackingTokens { get; set; }
    public DbSet<TaskInvitation> TaskInvitations { get; set; }

    // â”€â”€ Phase 3: Project Management â”€â”€
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectInvitation> ProjectInvitations { get; set; }
    public DbSet<ProjectActivityLog> ProjectActivityLogs { get; set; }

    // ── Phase 5: Productivity ──
    public DbSet<TimeEntry> TimeEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // ENUM CONVERSIONS â€” store as string in DB
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        modelBuilder.Entity<AppUser>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<TaskActivityLog>()
            .Property(l => l.Action)
            .HasConversion<string>();

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.Status)
            .HasConversion<string>()
            .HasDefaultValue(TaskItemStatus.Todo);

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.Priority)
            .HasConversion<string>()
            .HasDefaultValue(TaskItemPriority.Medium);

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.RepeatType)
            .HasConversion<string>()
            .HasDefaultValue(RepeatType.None);

        modelBuilder.Entity<TeamMember>()
            .Property(tm => tm.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Project>()
            .Property(p => p.Status)
            .HasConversion<string>()
            .HasDefaultValue(ProjectStatus.Planning);

        modelBuilder.Entity<Project>()
            .Property(p => p.Visibility)
            .HasConversion<string>()
            .HasDefaultValue(ProjectVisibility.Private);

        modelBuilder.Entity<Project>()
            .Property(p => p.ProjectType)
            .HasConversion<string>()
            .HasSentinel(ProjectType.Team)
            .HasDefaultValue(ProjectType.Team);

        modelBuilder.Entity<ProjectMember>()
            .Property(pm => pm.Role)
            .HasConversion<string>();

        modelBuilder.Entity<ProjectInvitation>()
            .Property(pi => pi.Role)
            .HasConversion<string>();

        modelBuilder.Entity<ProjectActivityLog>()
            .Property(l => l.Action)
            .HasConversion<string>();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PHASE 1 RELATIONSHIPS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        // AppUser â†’ Boards (Cascade)
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.Boards)
            .WithOne(b => b.Owner)
            .HasForeignKey(b => b.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // AppUser â†’ RefreshTokens (Cascade)
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Board â†’ Lists (Cascade)
        modelBuilder.Entity<Board>()
            .HasMany(b => b.Lists)
            .WithOne(l => l.Board)
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        // BoardList â†’ Tasks (Cascade)
        modelBuilder.Entity<BoardList>()
            .HasMany(l => l.Tasks)
            .WithOne(t => t.List)
            .HasForeignKey(t => t.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskItem â†’ Owner (NoAction â€” avoid multiple cascade)
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Owner)
            .WithMany(u => u.OwnedTasks)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        // TaskItem â†’ AssignedTo (NoAction)
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.NoAction);

        // AuditLog â†’ User (NoAction)
        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PHASE 2 RELATIONSHIPS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        // Team â†’ CreatedBy (NoAction)
        modelBuilder.Entity<Team>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

        // Team â†’ Members (Cascade)
        modelBuilder.Entity<Team>()
            .HasMany(t => t.Members)
            .WithOne(tm => tm.Team)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // TeamMember â†’ User (NoAction â€” avoid multiple cascade)
        modelBuilder.Entity<TeamMember>()
            .HasOne(tm => tm.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // TeamMember: unique constraint (UserId + TeamId)
        modelBuilder.Entity<TeamMember>()
            .HasIndex(tm => new { tm.TeamId, tm.UserId })
            .IsUnique();

        // TaskItem â†’ Team (NoAction, nullable)
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Team)
            .WithMany(team => team.Tasks)
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.NoAction);

        // Comment â†’ TaskItem (Cascade)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comment â†’ User (NoAction)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // TaskAttachment â†’ TaskItem (Cascade)
        modelBuilder.Entity<TaskAttachment>()
            .HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskAttachment â†’ User (NoAction)
        modelBuilder.Entity<TaskAttachment>()
            .HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // TaskActivityLog â†’ TaskItem (Cascade)
        modelBuilder.Entity<TaskActivityLog>()
            .HasOne(l => l.Task)
            .WithMany(t => t.ActivityLogs)
            .HasForeignKey(l => l.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskActivityLog â†’ User (NoAction)
        modelBuilder.Entity<TaskActivityLog>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // TaskInvitation â†’ TaskItem (Cascade)
        modelBuilder.Entity<TaskInvitation>()
            .HasOne(i => i.Task)
            .WithMany()
            .HasForeignKey(i => i.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskInvitation â†’ InvitedByUser (NoAction)
        modelBuilder.Entity<TaskInvitation>()
            .HasOne(i => i.InvitedByUser)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<NotificationType>()
            .HasIndex(nt => nt.Code)
            .IsUnique();

        modelBuilder.Entity<NotificationType>()
            .HasData(new NotificationType
            {
                Id = 1,
                Code = "GENERAL",
                Name = "General",
                Description = "General TaskHub notifications",
                DefaultChannels = "InApp,Email",
                IsActive = true,
                CreatedAt = new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<NotificationTemplate>()
            .HasOne(t => t.NotificationType)
            .WithMany(nt => nt.Templates)
            .HasForeignKey(t => t.NotificationTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserNotificationPreference>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserNotificationPreference>()
            .HasOne(p => p.NotificationType)
            .WithMany(nt => nt.UserPreferences)
            .HasForeignKey(p => p.NotificationTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserNotificationPreference>()
            .HasIndex(p => new { p.UserId, p.NotificationTypeId })
            .IsUnique();

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.NotificationType)
            .WithMany()
            .HasForeignKey(n => n.NotificationTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.CreatedAt });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.IsRead)
            .HasFilter("[IsRead] = 0");

        modelBuilder.Entity<NotificationDelivery>()
            .HasOne(d => d.Notification)
            .WithMany(n => n.Deliveries)
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotificationDelivery>()
            .HasIndex(d => new { d.Status, d.NextRetryAt });

        modelBuilder.Entity<NotificationDelivery>()
            .HasIndex(d => new { d.Channel, d.CreatedAt });

        modelBuilder.Entity<EmailTrackingToken>()
            .HasKey(t => t.Token);

        modelBuilder.Entity<EmailTrackingToken>()
            .HasOne(t => t.Delivery)
            .WithMany(d => d.EmailTrackingTokens)
            .HasForeignKey(t => t.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PHASE 3 RELATIONSHIPS: Project Management
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        // Project â†’ Owner (NoAction)
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.OwnedProjects)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Project â†’ Workspace/Team (SetNull for Personal Projects)
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Workspace)
            .WithMany()
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Project â†’ Members (Cascade)
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Members)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectMember â†’ User (NoAction)
        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ProjectMember: unique constraint (UserId + ProjectId)
        modelBuilder.Entity<ProjectMember>()
            .HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        // Project â†’ Invitations (Cascade)
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Invitations)
            .WithOne(pi => pi.Project)
            .HasForeignKey(pi => pi.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectInvitation â†’ InvitedByUser (NoAction)
        modelBuilder.Entity<ProjectInvitation>()
            .HasOne(pi => pi.InvitedByUser)
            .WithMany()
            .HasForeignKey(pi => pi.InvitedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Project â†’ ActivityLogs (Cascade)
        modelBuilder.Entity<Project>()
            .HasMany(p => p.ActivityLogs)
            .WithOne(l => l.Project)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectActivityLog â†’ User (NoAction)
        modelBuilder.Entity<ProjectActivityLog>()
            .HasOne(l => l.User)
            .WithMany(u => u.ProjectActivityLogs)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Board â†’ Project (SetNull - keep boards even if project deleted?) 
        // Or Cascade? Let's go with NoAction to be safe for now.
        modelBuilder.Entity<Board>()
            .HasOne(b => b.Project)
            .WithMany(p => p.Boards)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // INDEXES
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        modelBuilder.Entity<ProjectInvitation>()
            .HasIndex(pi => pi.Token)
            .IsUnique();

        modelBuilder.Entity<TaskInvitation>()
            .HasIndex(ti => ti.Token)
            .IsUnique();

        // ═══════════════════════════════════════
        // PHASE 5 RELATIONSHIPS: Productivity
        // ═══════════════════════════════════════

        // TimeEntry → TaskItem (Cascade)
        modelBuilder.Entity<TimeEntry>()
            .HasOne(te => te.Task)
            .WithMany()
            .HasForeignKey(te => te.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // TimeEntry → User (NoAction)
        modelBuilder.Entity<TimeEntry>()
            .HasOne(te => te.User)
            .WithMany()
            .HasForeignKey(te => te.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // TimeEntry indexes
        modelBuilder.Entity<TimeEntry>()
            .HasIndex(te => new { te.UserId, te.StartTime });

        modelBuilder.Entity<TimeEntry>()
            .HasIndex(te => new { te.TaskId, te.StartTime });

    }
}





