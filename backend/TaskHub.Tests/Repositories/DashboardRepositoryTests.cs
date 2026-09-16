using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Repositories;

public class DashboardRepositoryTests
{
    [Fact]
    public async Task GetStatsAsync_PersonalScope_IncludesOnlyCurrentUsersPersonalProjectTasks()
    {
        await using var context = CreateContext();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, userAId);
        SeedUser(context, userBId);
        SeedTeam(context, teamId, userAId);
        SeedProjectTask(context, userAId, ProjectType.Personal, status: TaskItemStatus.Todo);
        SeedProjectTask(context, userBId, ProjectType.Personal, status: TaskItemStatus.Done);
        SeedProjectTask(context, userAId, ProjectType.Team, workspaceId: teamId, assignedToId: userAId, status: TaskItemStatus.InProgress);
        await context.SaveChangesAsync();
        var repository = new DashboardRepository(context);

        var stats = await repository.GetStatsAsync(new DashboardScopeCriteria
        {
            UserId = userAId,
            Scope = DashboardScope.Personal
        });

        stats.Total.Should().Be(1);
        stats.Todo.Should().Be(1);
        stats.InProgress.Should().Be(0);
        stats.Done.Should().Be(0);
        stats.TotalBoards.Should().Be(1);
        stats.TeamMembers.Should().Be(0);
    }

    [Fact]
    public async Task GetStatsAsync_PersonalScope_UsesProjectHierarchyInsteadOfTaskOwner()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, userId);
        SeedUser(context, otherOwnerId);
        SeedTeam(context, teamId, otherOwnerId, userId);
        SeedProjectTask(context, otherOwnerId, ProjectType.Team, workspaceId: teamId, ownerIdOverride: userId);
        await context.SaveChangesAsync();
        var repository = new DashboardRepository(context);

        var stats = await repository.GetStatsAsync(new DashboardScopeCriteria
        {
            UserId = userId,
            Scope = DashboardScope.Personal
        });

        stats.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetStatsAsync_TeamScope_IncludesSelectedTeamAndExcludesOtherTeamAndPersonalTasks()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var teamAId = Guid.NewGuid();
        var teamBId = Guid.NewGuid();
        SeedUser(context, userId);
        SeedTeam(context, teamAId, userId);
        SeedTeam(context, teamBId, userId);
        SeedProjectTask(context, userId, ProjectType.Team, workspaceId: teamAId, status: TaskItemStatus.InProgress);
        SeedProjectTask(context, userId, ProjectType.Team, workspaceId: teamBId, status: TaskItemStatus.Done);
        SeedProjectTask(context, userId, ProjectType.Personal, status: TaskItemStatus.Todo);
        await context.SaveChangesAsync();
        var repository = new DashboardRepository(context);

        var stats = await repository.GetStatsAsync(new DashboardScopeCriteria
        {
            UserId = userId,
            Scope = DashboardScope.Team,
            TeamId = teamAId
        });

        stats.Total.Should().Be(1);
        stats.InProgress.Should().Be(1);
        stats.Done.Should().Be(0);
        stats.Todo.Should().Be(0);
        stats.TotalBoards.Should().Be(1);
        stats.TeamMembers.Should().Be(1);
    }

    [Fact]
    public async Task GetStatsAsync_TeamScope_ExcludesProjectsUserCannotAccess()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, outsiderId);
        SeedTeam(context, teamId, ownerId);
        SeedProjectTask(context, ownerId, ProjectType.Team, workspaceId: teamId);
        await context.SaveChangesAsync();
        var repository = new DashboardRepository(context);

        var stats = await repository.GetStatsAsync(new DashboardScopeCriteria
        {
            UserId = outsiderId,
            Scope = DashboardScope.Team,
            TeamId = teamId
        });

        stats.Total.Should().Be(0);
        stats.TotalBoards.Should().Be(0);
    }

    [Fact]
    public async Task GetStatsAsync_EmptyPersonalScope_ReturnsZeroValues()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        await context.SaveChangesAsync();
        var repository = new DashboardRepository(context);

        var stats = await repository.GetStatsAsync(new DashboardScopeCriteria
        {
            UserId = userId,
            Scope = DashboardScope.Personal
        });

        stats.Total.Should().Be(0);
        stats.Todo.Should().Be(0);
        stats.InProgress.Should().Be(0);
        stats.Done.Should().Be(0);
        stats.Overdue.Should().Be(0);
        stats.TotalBoards.Should().Be(0);
        stats.TeamMembers.Should().Be(0);
    }

    private static TaskItem SeedProjectTask(
        AppDbContext context,
        Guid ownerId,
        ProjectType projectType,
        Guid? workspaceId = null,
        Guid? assignedToId = null,
        Guid? ownerIdOverride = null,
        TaskItemStatus status = TaskItemStatus.Todo)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Project {Guid.NewGuid():N}",
            Slug = $"project-{Guid.NewGuid():N}",
            OwnerId = ownerId,
            ProjectType = projectType,
            WorkspaceId = workspaceId
        };
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Main Board",
            OwnerId = ownerId,
            ProjectId = project.Id
        };
        var list = new BoardList
        {
            Id = Guid.NewGuid(),
            Name = "To Do",
            BoardId = board.Id,
            Position = 1
        };
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            OwnerId = ownerIdOverride ?? ownerId,
            AssignedToId = assignedToId,
            ListId = list.Id,
            Status = status
        };

        context.Projects.Add(project);
        context.Boards.Add(board);
        context.Lists.Add(list);
        context.Tasks.Add(task);

        return task;
    }

    private static void SeedTeam(AppDbContext context, Guid teamId, Guid ownerId, params Guid[] memberIds)
    {
        context.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedById = ownerId });
        context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = ownerId, Role = TeamRole.Owner });
        foreach (var memberId in memberIds)
        {
            context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = memberId, Role = TeamRole.Member });
        }
    }

    private static void SeedUser(AppDbContext context, Guid userId)
    {
        context.Users.Add(new AppUser
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            FullName = "Test User",
            PasswordHash = "hash"
        });
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
