using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TaskHub.Application.Hubs;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Services;

public class RealtimeAuthorizationTests
{
    [Fact]
    public async Task AccessService_AuthorizesOwnerAndRejectsUnrelatedUserForBoardAndTask()
    {
        await using var db = CreateContext();
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        AddUser(db, owner);
        AddUser(db, outsider);
        var project = new Project { Name = "Personal", Slug = "personal", OwnerId = owner, ProjectType = ProjectType.Personal };
        var board = new Board { Name = "Board", OwnerId = owner, ProjectId = project.Id };
        var list = new BoardList { Name = "List", BoardId = board.Id };
        var task = new TaskItem { Title = "Task", OwnerId = owner, ListId = list.Id };
        db.Projects.Add(project);
        db.Boards.Add(board);
        db.Lists.Add(list);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.AuthorizeBoardAsync(owner, board.Id);
        await service.AuthorizeTaskAsync(owner, task.Id);
        await FluentActions.Invoking(() => service.AuthorizeBoardAsync(outsider, board.Id)).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Invoking(() => service.AuthorizeTaskAsync(outsider, task.Id)).Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AccessService_AllowsTeamMemberAndGuestProjectViewButRejectsOutsider()
    {
        await using var db = CreateContext();
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var guest = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        foreach (var id in new[] { owner, member, guest, outsider }) AddUser(db, id);
        var team = new Team { Name = "Team", CreatedById = owner };
        db.Teams.Add(team);
        db.TeamMembers.AddRange(
            new TeamMember { TeamId = team.Id, UserId = owner, Role = TeamRole.Owner },
            new TeamMember { TeamId = team.Id, UserId = member, Role = TeamRole.Member });
        var project = new Project { Name = "Team project", Slug = "team", OwnerId = owner, ProjectType = ProjectType.Team, WorkspaceId = team.Id };
        var board = new Board { Name = "Board", OwnerId = owner, ProjectId = project.Id };
        var list = new BoardList { Name = "List", BoardId = board.Id };
        var task = new TaskItem { Title = "Task", OwnerId = owner, ListId = list.Id };
        db.Projects.Add(project);
        db.Boards.Add(board);
        db.Lists.Add(list);
        db.Tasks.Add(task);
        db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = guest, Role = ProjectRole.Guest });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.AuthorizeProjectAsync(member, project.Id);
        await service.AuthorizeProjectAsync(guest, project.Id);
        await service.AuthorizeBoardAsync(guest, board.Id);
        await service.AuthorizeTaskAsync(guest, task.Id);
        await service.AuthorizeTeamAsync(member, team.Id);
        await FluentActions.Invoking(() => service.AuthorizeProjectAsync(outsider, project.Id)).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Invoking(() => service.AuthorizeTeamAsync(outsider, team.Id)).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Invoking(() => service.AuthorizeBoardAsync(outsider, board.Id)).Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HubJoinBoard_RejectsUnauthorizedBeforeJoiningGroup()
    {
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var access = Substitute.For<IRealtimeAccessService>();
        access.AuthorizeBoardAsync(userId, boardId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Denied")));
        var context = Substitute.For<HubCallerContext>();
        context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test")));
        context.ConnectionId.Returns("test-connection");
        var groups = Substitute.For<IGroupManager>();
        var hub = new NotificationHub(access) { Context = context, Groups = groups };

        await FluentActions.Invoking(() => hub.JoinBoard(boardId.ToString())).Should().ThrowAsync<ForbiddenException>();
        await groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HubJoinTask_AddsAuthorizedConnectionToTaskGroup()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var access = Substitute.For<IRealtimeAccessService>();
        access.AuthorizeTaskAsync(userId, taskId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var context = Substitute.For<HubCallerContext>();
        context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test")));
        context.ConnectionId.Returns("test-connection");
        var groups = Substitute.For<IGroupManager>();
        groups.AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var hub = new NotificationHub(access) { Context = context, Groups = groups };

        await hub.JoinTask(taskId.ToString());

        await groups.Received(1).AddToGroupAsync("test-connection", $"task_{taskId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HubJoinProjectAndTeam_RequiresAuthorizationBeforeAddingGroups()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var access = Substitute.For<IRealtimeAccessService>();
        access.AuthorizeProjectAsync(userId, projectId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        access.AuthorizeTeamAsync(userId, teamId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Denied")));
        var context = Substitute.For<HubCallerContext>();
        context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test")));
        context.ConnectionId.Returns("test-connection");
        var groups = Substitute.For<IGroupManager>();
        groups.AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var hub = new NotificationHub(access) { Context = context, Groups = groups };

        await hub.JoinProject(projectId.ToString());
        await FluentActions.Invoking(() => hub.JoinTeam(teamId.ToString())).Should().ThrowAsync<ForbiddenException>();

        await groups.Received(1).AddToGroupAsync("test-connection", $"project_{projectId}", Arg.Any<CancellationToken>());
        await groups.DidNotReceive().AddToGroupAsync("test-connection", $"team_{teamId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GuestCannotCommentOnTeamTaskEvenWhenTheyOwnTheTask()
    {
        await using var db = CreateContext();
        var owner = Guid.NewGuid();
        var guest = Guid.NewGuid();
        AddUser(db, owner);
        AddUser(db, guest);
        var team = new Team { Name = "Team", CreatedById = owner };
        var project = new Project { Name = "Project", Slug = "project", OwnerId = owner, ProjectType = ProjectType.Team, WorkspaceId = team.Id };
        var board = new Board { Name = "Board", OwnerId = owner, ProjectId = project.Id };
        var list = new BoardList { Name = "List", BoardId = board.Id };
        var task = new TaskItem { Title = "Task", OwnerId = guest, ListId = list.Id };
        db.Teams.Add(team);
        db.Projects.Add(project);
        db.Boards.Add(board);
        db.Lists.Add(list);
        db.Tasks.Add(task);
        db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = guest, Role = ProjectRole.Guest });
        await db.SaveChangesAsync();
        var permissions = new PermissionService(new ProjectRepository(db), new TeamRepository(db), new UserRepository(db));
        var service = new CommentService(new CommentRepository(db), permissions,
            Substitute.For<IAuditService>(), Substitute.For<IHubContext<NotificationHub>>());

        await FluentActions.Invoking(() => service.CreateCommentAsync(task.Id,
            new CreateCommentDto { Content = "Should fail" }, guest)).Should().ThrowAsync<ForbiddenException>();
        db.Comments.Should().BeEmpty();
        db.TaskActivityLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CommentCreation_PersistsActivityBeforeBroadcast()
    {
        await using var db = CreateContext();
        var owner = Guid.NewGuid();
        AddUser(db, owner);
        var project = new Project { Name = "Personal", Slug = "personal", OwnerId = owner, ProjectType = ProjectType.Personal };
        var board = new Board { Name = "Board", OwnerId = owner, ProjectId = project.Id };
        var list = new BoardList { Name = "List", BoardId = board.Id };
        var task = new TaskItem { Title = "Task", OwnerId = owner, ListId = list.Id };
        db.Projects.Add(project);
        db.Boards.Add(board);
        db.Lists.Add(list);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        var hub = Substitute.For<IHubContext<NotificationHub>>();
        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.Group(Arg.Any<string>()).Returns(proxy);
        proxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                db.TaskActivityLogs.Count(log => log.TaskId == task.Id && log.Action == ActivityLogAction.Commented)
                    .Should().Be(1);
                return Task.CompletedTask;
            });
        var permissions = new PermissionService(new ProjectRepository(db), new TeamRepository(db), new UserRepository(db));
        var service = new CommentService(new CommentRepository(db), permissions,
            Substitute.For<IAuditService>(), hub);

        await service.CreateCommentAsync(task.Id, new CreateCommentDto { Content = "Hello" }, owner);

        db.Comments.Should().ContainSingle();
        db.TaskActivityLogs.Single(log => log.TaskId == task.Id).Action.Should().Be(ActivityLogAction.Commented);
        await proxy.Received(1).SendCoreAsync("ProjectActivity", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    private static IRealtimeAccessService CreateService(AppDbContext db)
    {
        var projects = new ProjectRepository(db);
        var teams = new TeamRepository(db);
        var users = new UserRepository(db);
        var permissions = new PermissionService(projects, teams, users);
        return new RealtimeAccessService(new BoardRepository(db), new TaskItemRepository(db), projects, teams, users, permissions);
    }

    private static void AddUser(AppDbContext db, Guid id) => db.Users.Add(new AppUser
    {
        Id = id, FullName = "User", Email = $"{id:N}@example.com", PasswordHash = "hash"
    });

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
