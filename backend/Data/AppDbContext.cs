using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Models;

namespace TaskHub.backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Board> Boards { get; set; }
    public DbSet<BoardList> Lists { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Store UserRole enum as string
        modelBuilder.Entity<AppUser>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // AppUser → Boards
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.Boards)
            .WithOne(b => b.Owner)
            .HasForeignKey(b => b.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // AppUser → RefreshTokens
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Board → Lists
        modelBuilder.Entity<Board>()
            .HasMany(b => b.Lists)
            .WithOne(l => l.Board)
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        // BoardList → Tasks
        modelBuilder.Entity<BoardList>()
            .HasMany(l => l.Tasks)
            .WithOne(t => t.List)
            .HasForeignKey(t => t.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskItem → Owner (no cascade to avoid multiple cascade paths)
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Owner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        // AuditLog → User (no cascade)
        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();
    }
}
