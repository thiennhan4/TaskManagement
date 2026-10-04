using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Repositories;

public class TaskCollectionAuthorizationTests
{
    [Fact]
    public async Task GetTasksAsync_ExcludesOtherUsersPersonalProjectTasks()
    {
        await using var context = CreateContext();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var userATaskId = SeedProjectTask(context, userAId, ProjectType.Personal).Id;
        var userBTaskId = SeedProjectTask(context, userBId, ProjectType.Personal).Id;
        await context.SaveChangesAsync();
        var repository = new TaskItemRepository(context);

        var (tasks, totalCount) = await repository.GetTasksAsync(new TaskFilterDto(), userAId, isAdmin: false);

        tasks.Should().ContainSingle(t => t.Id == userATaskId);
        tasks.Should().NotContain(t => t.Id == userBTaskId);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsTasksForProjectMembers()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var expectedTaskId = SeedProjectTask(context, ownerId, ProjectType.Team, memberId, ProjectRole.Member).Id;
        await context.SaveChangesAsync();
        var repository = new TaskItemRepository(context);

        var (tasks, totalCount) = await repository.GetTasksAsync(new TaskFilterDto(), memberId, isAdmin: false);

        tasks.Should().ContainSingle(t => t.Id == expectedTaskId);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTasksAsync_ExcludesUnauthorizedProjectTasks()
    {
        await using var context = CreateContext();
        var memberId = Guid.NewGuid();
        var allowedTaskId = SeedProjectTask(context, Guid.NewGuid(), ProjectType.Team, memberId, ProjectRole.Member).Id;
        var unauthorizedTaskId = SeedProjectTask(context, Guid.NewGuid(), ProjectType.Team).Id;
        await context.SaveChangesAsync();
        var repository = new TaskItemRepository(context);

        var (tasks, totalCount) = await repository.GetTasksAsync(new TaskFilterDto(), memberId, isAdmin: false);

        tasks.Should().ContainSingle(t => t.Id == allowedTaskId);
        tasks.Should().NotContain(t => t.Id == unauthorizedTaskId);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTasksAsync_PreservesAssignedAndOwnedTaskAccess()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var ownedTaskId = SeedLegacyTask(context, ownerId: userId).Id;
        var assignedTaskId = SeedLegacyTask(context, ownerId: otherUserId, assignedToId: userId).Id;
        var privateTaskId = SeedLegacyTask(context, ownerId: otherUserId).Id;
        await context.SaveChangesAsync();
        var repository = new TaskItemRepository(context);

        var (tasks, totalCount) = await repository.GetTasksAsync(new TaskFilterDto(), userId, isAdmin: false);

        tasks.Should().Contain(t => t.Id == ownedTaskId);
        tasks.Should().Contain(t => t.Id == assignedTaskId);
        tasks.Should().NotContain(t => t.Id == privateTaskId);
        totalCount.Should().Be(2);
    }

    private static TaskItem SeedProjectTask(
        AppDbContext context,
        Guid ownerId,
        ProjectType projectType,
        Guid? memberId = null,
        ProjectRole? memberRole = null)
    {
        EnsureUser(context, ownerId);
        if (memberId.HasValue)
        {
            EnsureUser(context, memberId.Value);
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Project {Guid.NewGuid():N}",
            Slug = $"project-{Guid.NewGuid():N}",
            OwnerId = ownerId,
            ProjectType = projectType,
            WorkspaceId = projectType == ProjectType.Team ? Guid.NewGuid() : null
        };
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Board",
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
            OwnerId = ownerId,
            ListId = list.Id
        };

        context.Projects.Add(project);
        context.Boards.Add(board);
        context.Lists.Add(list);
        context.Tasks.Add(task);

        if (memberId.HasValue && memberRole.HasValue)
        {
            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = memberId.Value,
                Role = memberRole.Value
            });
        }

        return task;
    }

    private static TaskItem SeedLegacyTask(AppDbContext context, Guid ownerId, Guid? assignedToId = null)
    {
        EnsureUser(context, ownerId);
        if (assignedToId.HasValue)
        {
            EnsureUser(context, assignedToId.Value);
        }

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Legacy Board",
            OwnerId = ownerId
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
            Title = "Legacy Task",
            OwnerId = ownerId,
            AssignedToId = assignedToId,
            ListId = list.Id
        };

        context.Boards.Add(board);
        context.Lists.Add(list);
        context.Tasks.Add(task);

        return task;
    }

    private static void EnsureUser(AppDbContext context, Guid userId)
    {
        if (context.Users.Local.Any(u => u.Id == userId))
        {
            return;
        }

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
