using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TaskHub.Application.DTOs;
using TaskHub.Application.Hubs;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Services;

public class TaskAssignmentBusinessRulesTests
{
    [Fact]
    public async Task TaskActivity_BroadcastsAfterCreateAndNotAfterRejectedCreate()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var (projectId, listId) = SeedProjectBoard(context, owner, ProjectType.Personal);
        SeedUser(context, outsider);
        await context.SaveChangesAsync();
        var hub = Substitute.For<IProtectedHubContext>();
        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.Group(Arg.Any<string>()).Returns(proxy);
        proxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var service = CreateService(context, hub);

        await service.CreateTaskAsync(listId, new CreateTaskDto { Title = "Created" }, owner);
        await proxy.Received(1).SendCoreAsync("ProjectActivity", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
        _ = clients.Received().Group($"project_{projectId}");

        await FluentActions.Invoking(() => service.CreateTaskAsync(listId,
            new CreateTaskDto { Title = "Rejected" }, outsider)).Should().ThrowAsync<ForbiddenException>();
        await proxy.Received(1).SendCoreAsync("ProjectActivity", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
        context.TaskActivityLogs.Count(log => log.Action == ActivityLogAction.Created).Should().Be(1);
    }

    [Fact]
    public async Task UpdateTaskAsync_StatusChange_RecordsOneCompletionEvent()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var listId = SeedProjectBoard(context, ownerId, ProjectType.Personal).ListId;
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var task = await service.CreateTaskAsync(listId, new CreateTaskDto { Title = "Task" }, ownerId);
        var update = new UpdateTaskDto { Title = "Task", Status = TaskItemStatus.Done, AssignedToId = ownerId };

        await service.UpdateTaskAsync(task.Id, update, ownerId);
        await service.UpdateTaskAsync(task.Id, update, ownerId);

        var completions = await context.TaskActivityLogs
            .Where(log => log.TaskId == task.Id && log.Action == ActivityLogAction.StatusChanged && log.NewValue == "Done")
            .ToListAsync();
        completions.Should().ContainSingle();
        completions[0].OldValue.Should().Be("Todo");
    }

    [Fact]
    public async Task UpdateTaskAsync_AssigneeChange_RecordsOneAssignmentEvent()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var seeded = SeedProjectBoard(context, ownerId, ProjectType.Team);
        SeedUser(context, memberId);
        context.ProjectMembers.Add(new ProjectMember { ProjectId = seeded.ProjectId, UserId = memberId, Role = ProjectRole.Member });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var task = await service.CreateTaskAsync(seeded.ListId, new CreateTaskDto { Title = "Task" }, ownerId);
        var update = new UpdateTaskDto { Title = "Task", AssignedToId = memberId };

        await service.UpdateTaskAsync(task.Id, update, ownerId);
        await service.UpdateTaskAsync(task.Id, update, ownerId);

        var assignments = await context.TaskActivityLogs
            .Where(log => log.TaskId == task.Id && log.Action == ActivityLogAction.Assigned)
            .ToListAsync();
        assignments.Should().ContainSingle();
        assignments[0].OldValue.Should().BeNull();
        assignments[0].NewValue.Should().Be(memberId.ToString());
    }

    [Fact]
    public async Task AssignTaskAsync_NoOp_DoesNotRecordDuplicateActivity()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var seeded = SeedProjectBoard(context, ownerId, ProjectType.Team);
        SeedUser(context, memberId);
        context.ProjectMembers.Add(new ProjectMember { ProjectId = seeded.ProjectId, UserId = memberId, Role = ProjectRole.Member });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var task = await service.CreateTaskAsync(seeded.ListId, new CreateTaskDto { Title = "Task" }, ownerId);

        await service.AssignTaskAsync(task.Id, new AssignTaskDto { AssignedToUserId = memberId }, ownerId);
        await service.AssignTaskAsync(task.Id, new AssignTaskDto { AssignedToUserId = memberId }, ownerId);

        (await context.TaskActivityLogs.CountAsync(log => log.TaskId == task.Id && log.Action == ActivityLogAction.Assigned))
            .Should().Be(1);
    }

    [Fact]
    public async Task UpdateTaskAsync_RejectedAssignee_DoesNotRecordStatusOrAssignmentActivity()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var seeded = SeedProjectBoard(context, ownerId, ProjectType.Team);
        SeedUser(context, outsiderId);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var task = await service.CreateTaskAsync(seeded.ListId, new CreateTaskDto { Title = "Task" }, ownerId);

        await FluentActions.Invoking(() => service.UpdateTaskAsync(task.Id,
            new UpdateTaskDto { Title = "Task", Status = TaskItemStatus.Done, AssignedToId = outsiderId }, ownerId))
            .Should().ThrowAsync<BusinessValidationException>();

        (await context.Tasks.SingleAsync(item => item.Id == task.Id)).Status.Should().Be(TaskItemStatus.Todo);
        context.TaskActivityLogs.Where(log => log.TaskId == task.Id)
            .Select(log => log.Action).Should().Equal(ActivityLogAction.Created);
    }

    [Fact]
    public async Task CreateTaskAsync_PersonalProjectDefaultsAssigneeToOwner()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var listId = SeedProjectBoard(context, ownerId, ProjectType.Personal).ListId;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateTaskAsync(listId, new CreateTaskDto { Title = "Task" }, ownerId);

        result.AssignedToId.Should().Be(ownerId);
        (await context.Tasks.SingleAsync(t => t.Id == result.Id)).AssignedToId.Should().Be(ownerId);
    }

    [Fact]
    public async Task CreateTaskAsync_PersonalProjectRejectsArbitraryAssignee()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var listId = SeedProjectBoard(context, ownerId, ProjectType.Personal).ListId;
        SeedUser(context, outsiderId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.CreateTaskAsync(listId, new CreateTaskDto
        {
            Title = "Task",
            AssignedToId = outsiderId
        }, ownerId);

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task CreateTaskAsync_TeamProjectAllowsEligibleAssignee()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var seeded = SeedProjectBoard(context, ownerId, ProjectType.Team);
        SeedUser(context, memberId);
        context.ProjectMembers.Add(new ProjectMember { ProjectId = seeded.ProjectId, UserId = memberId, Role = ProjectRole.Member });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateTaskAsync(seeded.ListId, new CreateTaskDto
        {
            Title = "Task",
            AssignedToId = memberId
        }, ownerId);

        result.AssignedToId.Should().Be(memberId);
    }

    [Fact]
    public async Task CreateTaskAsync_TeamProjectRejectsUnrelatedAssignee()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var seeded = SeedProjectBoard(context, ownerId, ProjectType.Team);
        SeedUser(context, outsiderId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.CreateTaskAsync(seeded.ListId, new CreateTaskDto
        {
            Title = "Task",
            AssignedToId = outsiderId
        }, ownerId);

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task DetailCapabilitiesAndEligibleAssignees_UsePermissionsAndRejectMissingTasks()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var listId = SeedProjectBoard(context, owner, ProjectType.Personal).ListId;
        SeedUser(context, outsider);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var task = await service.CreateTaskAsync(listId, new CreateTaskDto { Title = "Canonical" }, owner);
        var detail = await service.GetTaskByIdAsync(task.Id, owner);
        detail.Capabilities.CanEdit.Should().BeTrue();
        detail.Capabilities.CanAssign.Should().BeTrue();
        var assignees = await service.GetEligibleAssigneesAsync(task.Id, owner, new PageQueryDto { PageSize = 1 });
        assignees.Items.Should().ContainSingle().Which.Id.Should().Be(owner);
        await FluentActions.Invoking(() => service.GetEligibleAssigneesAsync(task.Id, outsider, new PageQueryDto())).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Invoking(() => service.GetTaskByIdAsync(Guid.NewGuid(), owner)).Should().ThrowAsync<NotFoundException>();
    }

    private static TaskItemService CreateService(AppDbContext context, IProtectedHubContext? hubOverride = null)
    {
        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .CreateNotificationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.CompletedTask);

        var hubContext = Substitute.For<IProtectedHubContext>();
        var hubClients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(hubClients);
        hubClients.Group(Arg.Any<string>()).Returns(clientProxy);
        clientProxy
            .SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var mutations10276 = new MutationRunner(context);
        return new TaskItemService(
            new TaskItemRepository(context),
            new BoardListRepository(context),
            new BoardRepository(context),
            new ProjectRepository(context),
            new PermissionService(new ProjectRepository(context), new TeamRepository(context), new UserRepository(context)),
            Substitute.For<IAuditService>(),
            new TaskCollaborationRepository(context),
            new UserRepository(context),
            new TeamRepository(context),
            Substitute.For<IAttachmentStorage>(),
            Substitute.For<IEmailService>(),
            notificationService,
            hubOverride ?? hubContext, mutations10276, new TaskHub.Application.Services.DurableDelivery(new OutboxRepository(context), mutations10276, Microsoft.Extensions.Logging.Abstractions.NullLogger<TaskHub.Application.Services.DurableDelivery>.Instance));
    }

    private static (Guid ProjectId, Guid ListId) SeedProjectBoard(AppDbContext context, Guid ownerId, ProjectType type)
    {
        SeedUser(context, ownerId);
        var workspaceId = type == ProjectType.Team ? Guid.NewGuid() : (Guid?)null;
        if (workspaceId.HasValue)
        {
            context.Teams.Add(new Team { Id = workspaceId.Value, Name = "Workspace", CreatedById = ownerId });
            context.TeamMembers.Add(new TeamMember { TeamId = workspaceId.Value, UserId = ownerId, Role = TeamRole.Owner });
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project",
            Slug = $"project-{Guid.NewGuid():N}",
            OwnerId = ownerId,
            ProjectType = type,
            WorkspaceId = workspaceId
        };
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Main Board",
            OwnerId = ownerId,
            ProjectId = project.Id,
            Project = project
        };
        var list = new BoardList
        {
            Id = Guid.NewGuid(),
            Name = "To Do",
            BoardId = board.Id,
            Board = board,
            Position = 1
        };

        context.Projects.Add(project);
        context.Boards.Add(board);
        context.Lists.Add(list);

        return (project.Id, list.Id);
    }

    private static void SeedUser(AppDbContext context, Guid userId)
    {
        if (context.Users.Local.Any(u => u.Id == userId))
            return;

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
