using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TaskHub.Application.DTOs;
using TaskHub.Application.Hubs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Services;

public class ProjectBusinessRulesTests
{
    [Fact]
    public async Task ProjectTypeMapping_UsesTeamAsDatabaseDefaultSentinel()
    {
        await using var context = CreateContext();
        var property = context.Model.FindEntityType(typeof(Project))!.FindProperty(nameof(Project.ProjectType))!;

        property.Sentinel.Should().Be(ProjectType.Team);
        property.GetDefaultValue().Should().Be(ProjectType.Team);
    }

    [Fact]
    public async Task ProjectActivity_BroadcastsOnlyAfterSuccessfulPersistence()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        SeedUser(context, owner);
        await context.SaveChangesAsync();
        var hub = CreateHub();
        var service = CreateService(context, hub);

        await FluentActions.Invoking(() => service.CreateProjectAsync(new CreateProjectDto
        {
            ProjectType = ProjectType.Team, Name = "Invalid"
        }, owner)).Should().ThrowAsync<BadRequestException>();
        await hub.Clients.Group(Arg.Any<string>()).DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());

        var project = await service.CreateProjectAsync(new CreateProjectDto
        {
            ProjectType = ProjectType.Personal, Name = "Personal"
        }, owner);
        await hub.Clients.Group($"project_{project.Id}").Received(1).SendCoreAsync(
            "ProjectActivity", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectCreation_DoesNotWriteActivityWhenNotificationFails()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        SeedUser(context, owner);
        await context.SaveChangesAsync();
        var notifications = Substitute.For<INotificationService>();
        notifications.CreateNotificationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromException(new InvalidOperationException("Notification failed")));
        var service = CreateService(context, notificationServiceOverride: notifications);

        await FluentActions.Invoking(() => service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Personal", ProjectType = ProjectType.Personal
        }, owner)).Should().ThrowAsync<InvalidOperationException>();

        context.ProjectActivityLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertToTeamAsync_PreservesHierarchyAndMovesDashboardScope()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, owner);
        SeedTeam(context, teamId, owner);
        var project = SeedProject(context, owner, ProjectType.Personal);
        var board = new Board { Id = Guid.NewGuid(), ProjectId = project.Id, OwnerId = owner, Name = "Board" };
        var list = new BoardList { Id = Guid.NewGuid(), BoardId = board.Id, Name = "To Do" };
        var task = new TaskItem { Id = Guid.NewGuid(), ListId = list.Id, OwnerId = owner, AssignedToId = owner, Title = "Task" };
        context.Boards.Add(board);
        context.Lists.Add(list);
        context.Tasks.Add(task);
        context.TaskActivityLogs.Add(new TaskActivityLog { TaskId = task.Id, UserId = owner, Action = ActivityLogAction.Created });
        var comment = new Comment { TaskId = task.Id, UserId = owner, Content = "Keep this" };
        var timeEntry = new TimeEntry { TaskId = task.Id, UserId = owner, StartTime = DateTime.UtcNow, DurationSeconds = 300 };
        context.Comments.Add(comment);
        context.TimeEntries.Add(timeEntry);
        await context.SaveChangesAsync();
        var dashboard = new DashboardRepository(context);

        var converted = await CreateService(context).ConvertToTeamAsync(project.Id, teamId, owner);

        converted.ProjectType.Should().Be(ProjectType.Team);
        converted.WorkspaceId.Should().Be(teamId);
        context.Boards.Single(b => b.ProjectId == project.Id).Id.Should().Be(board.Id);
        context.Lists.Single(l => l.BoardId == board.Id).Id.Should().Be(list.Id);
        context.Tasks.Single(t => t.Id == task.Id).TeamId.Should().Be(teamId);
        context.Tasks.Single(t => t.Id == task.Id).AssignedToId.Should().Be(owner);
        context.TaskActivityLogs.Count(log => log.TaskId == task.Id).Should().Be(1);
        context.Comments.Single(entry => entry.TaskId == task.Id).Id.Should().Be(comment.Id);
        context.TimeEntries.Single(entry => entry.TaskId == task.Id).Id.Should().Be(timeEntry.Id);
        context.ProjectMembers.Single(member => member.ProjectId == project.Id && member.UserId == owner).Role.Should().Be(ProjectRole.Owner);
        (await dashboard.GetStatsAsync(new DashboardScopeCriteria { UserId = owner, Scope = DashboardScope.Personal })).Total.Should().Be(0);
        (await dashboard.GetStatsAsync(new DashboardScopeCriteria { UserId = owner, Scope = DashboardScope.Team, TeamId = teamId })).Total.Should().Be(1);
    }

    [Fact]
    public async Task ConvertToTeamAsync_RejectsNonOwnerAndUnauthorizedTeamWithoutChangingProject()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, owner);
        SeedUser(context, outsider);
        SeedTeam(context, teamId, outsider);
        var project = SeedProject(context, owner, ProjectType.Personal);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await FluentActions.Invoking(() => service.ConvertToTeamAsync(project.Id, teamId, outsider)).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Invoking(() => service.ConvertToTeamAsync(project.Id, teamId, owner)).Should().ThrowAsync<ForbiddenException>();
        project.ProjectType.Should().Be(ProjectType.Personal);
        project.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task ConvertToTeamAsync_RejectsMissingTeamWithoutChangingProject()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        SeedUser(context, owner);
        var project = SeedProject(context, owner, ProjectType.Personal);
        await context.SaveChangesAsync();

        await FluentActions.Invoking(() => CreateService(context).ConvertToTeamAsync(project.Id, Guid.NewGuid(), owner))
            .Should().ThrowAsync<NotFoundException>();

        project.ProjectType.Should().Be(ProjectType.Personal);
        project.WorkspaceId.Should().BeNull();
        context.ProjectActivityLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertToTeamAsync_RejectsAlreadyTeamAndInvalidAssignee()
    {
        await using var context = CreateContext();
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, owner);
        SeedUser(context, outsider);
        SeedTeam(context, teamId, owner);
        var project = SeedProject(context, owner, ProjectType.Personal);
        var board = new Board { ProjectId = project.Id, OwnerId = owner, Name = "Board" };
        var list = new BoardList { BoardId = board.Id, Name = "To Do" };
        context.Boards.Add(board);
        context.Lists.Add(list);
        context.Tasks.Add(new TaskItem { ListId = list.Id, OwnerId = owner, AssignedToId = outsider, Title = "Task" });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await FluentActions.Invoking(() => service.ConvertToTeamAsync(project.Id, teamId, owner)).Should().ThrowAsync<BusinessValidationException>();
        project.ProjectType.Should().Be(ProjectType.Personal);
        context.Tasks.Single().TeamId.Should().BeNull();

        project.ProjectType = ProjectType.Team;
        project.WorkspaceId = teamId;
        await context.SaveChangesAsync();
        await FluentActions.Invoking(() => service.ConvertToTeamAsync(project.Id, teamId, owner)).Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task CreateProjectAsync_PersonalProject_NormalizesWorkspaceAndUsesAuthenticatedOwner()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedTeam(context, workspaceId, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Personal",
            Slug = "personal",
            ProjectType = ProjectType.Personal,
            WorkspaceId = workspaceId,
            Visibility = ProjectVisibility.Public
        }, ownerId);

        result.OwnerId.Should().Be(ownerId);
        result.WorkspaceId.Should().BeNull();
        result.ProjectType.Should().Be(ProjectType.Personal);
        result.Visibility.Should().Be(ProjectVisibility.Private);
        context.ProjectMembers.Where(pm => pm.ProjectId == result.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProjectAsync_TeamProjectRequiresWorkspace()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Team",
            Slug = "team",
            ProjectType = ProjectType.Team
        }, userId);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CreateProjectAsync_DeniesUnauthorizedWorkspace()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, outsiderId);
        SeedTeam(context, workspaceId, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Team",
            Slug = "team",
            ProjectType = ProjectType.Team,
            WorkspaceId = workspaceId
        }, outsiderId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateProjectAsync_AuthorizedUserCreatesTeamProjectWithOwnerMember()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedTeam(context, workspaceId, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Team",
            Slug = "team",
            ProjectType = ProjectType.Team,
            WorkspaceId = workspaceId
        }, ownerId);

        result.ProjectType.Should().Be(ProjectType.Team);
        result.WorkspaceId.Should().Be(workspaceId);
        var member = await context.ProjectMembers.SingleAsync(pm => pm.ProjectId == result.Id && pm.UserId == ownerId);
        member.Role.Should().Be(ProjectRole.Owner);
    }

    [Fact]
    public async Task InviteMemberAsync_DeniesPersonalProjectInvites()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        SeedUser(context, ownerId);
        var project = SeedProject(context, ownerId, ProjectType.Personal);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.InviteMemberAsync(project.Id, new InviteMemberDto
        {
            Email = "invitee@example.com",
            Role = ProjectRole.Member
        }, ownerId);

        await act.Should().ThrowAsync<BusinessValidationException>();
        context.ProjectActivityLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptInvitationAsync_DeniesPersonalProjectMemberAddition()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, inviteeId, "invitee@example.com");
        var project = SeedProject(context, ownerId, ProjectType.Personal);
        var invitation = new ProjectInvitation
        {
            ProjectId = project.Id,
            Project = project,
            InvitedByUserId = ownerId,
            InviteeEmail = "invitee@example.com",
            Role = ProjectRole.Member
        };
        context.ProjectInvitations.Add(invitation);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AcceptInvitationAsync(invitation.Token, inviteeId);

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task InviteMemberAsync_AllowsTeamProjectInvitation()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, inviteeId, "invitee@example.com");
        SeedTeam(context, workspaceId, ownerId);
        var project = SeedProject(context, ownerId, ProjectType.Team, workspaceId);
        context.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = ownerId, Role = ProjectRole.Owner });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.InviteMemberAsync(project.Id, new InviteMemberDto
        {
            Email = "invitee@example.com",
            Role = ProjectRole.Member
        }, ownerId);

        var invitation = await context.ProjectInvitations.SingleAsync(i => i.ProjectId == project.Id);
        invitation.InviteeEmail.Should().Be("invitee@example.com");
        invitation.Role.Should().Be(ProjectRole.Member);
        (await context.ProjectActivityLogs.SingleAsync(log => log.ProjectId == project.Id))
            .Action.Should().Be(ProjectActivityAction.MemberInvited);

        await service.AcceptInvitationAsync(invitation.Token, inviteeId);

        (await context.ProjectMembers.SingleAsync(member => member.ProjectId == project.Id && member.UserId == inviteeId))
            .Role.Should().Be(ProjectRole.Member);
        (await context.ProjectActivityLogs.SingleAsync(log => log.ProjectId == project.Id && log.Action == ProjectActivityAction.MemberJoined))
            .UserId.Should().Be(inviteeId);
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesDefaultBoardAndListsOnce()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        SeedUser(context, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Personal",
            Slug = "personal",
            ProjectType = ProjectType.Personal
        }, ownerId);

        var repository = new ProjectRepository(context);
        var project = await repository.GetByIdAsync(result.Id);
        await repository.EnsureDefaultBoardStructureAsync(project!);

        var board = await context.Boards.SingleAsync(b => b.ProjectId == result.Id && b.Name == "Main Board");
        var listNames = await context.Lists
            .Where(l => l.BoardId == board.Id)
            .OrderBy(l => l.Position)
            .Select(l => l.Name)
            .ToListAsync();

        listNames.Should().Equal("To Do", "In Progress", "Done");
    }

    private static IProtectedHubContext CreateHub()
    {
        var hub = Substitute.For<IProtectedHubContext>();
        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.Group(Arg.Any<string>()).Returns(proxy);
        proxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return hub;
    }

    private static ProjectService CreateService(AppDbContext context, IProtectedHubContext? hub = null,
        INotificationService? notificationServiceOverride = null)
    {
        hub ??= CreateHub();
        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .CreateNotificationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.CompletedTask);

        var emailService = Substitute.For<IEmailService>();
        emailService
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);

        return new ProjectService(
            new ProjectRepository(context),
            new TeamRepository(context),
            new UserRepository(context),
            new PermissionService(new ProjectRepository(context), new TeamRepository(context), new UserRepository(context)),
            Substitute.For<IAuditService>(),
            emailService,
            notificationServiceOverride ?? notificationService,
            hub);
    }

    private static Project SeedProject(AppDbContext context, Guid ownerId, ProjectType type, Guid? workspaceId = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Project {Guid.NewGuid():N}",
            Slug = $"project-{Guid.NewGuid():N}",
            OwnerId = ownerId,
            ProjectType = type,
            WorkspaceId = workspaceId,
            Visibility = type == ProjectType.Personal ? ProjectVisibility.Private : ProjectVisibility.TeamOnly
        };
        context.Projects.Add(project);
        return project;
    }

    private static void SeedTeam(AppDbContext context, Guid teamId, Guid ownerId)
    {
        context.Teams.Add(new Team { Id = teamId, Name = "Workspace", CreatedById = ownerId });
        context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = ownerId, Role = TeamRole.Owner });
    }

    private static void SeedUser(AppDbContext context, Guid userId, string? email = null)
    {
        context.Users.Add(new AppUser
        {
            Id = userId,
            Email = email ?? $"{userId:N}@example.com",
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
