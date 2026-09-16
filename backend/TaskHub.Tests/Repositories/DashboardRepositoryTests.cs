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
    [Theory]
    [InlineData(DashboardTimeframe.Week, 2026, 9, 14, 7)]
    [InlineData(DashboardTimeframe.Month, 2026, 9, 1, 30)]
    [InlineData(DashboardTimeframe.SixMonths, 2026, 4, 1, 6)]
    [InlineData(DashboardTimeframe.Year, 2026, 1, 1, 12)]
    public async Task GetVelocityAsync_Timeframe_UsesStableUtcBucketsAndZeroFills(
        DashboardTimeframe timeframe, int year, int month, int day, int expectedCount)
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        await context.SaveChangesAsync();
        var asOf = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

        var points = await new DashboardRepository(context).GetVelocityAsync(
            new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal }, timeframe, asOf);

        points.Should().HaveCount(expectedCount);
        points[0].Start.Should().Be(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));
        points.Zip(points.Skip(1)).Should().OnlyContain(pair => pair.First.End == pair.Second.Start);
        points.Should().OnlyContain(point => point.Created == 0 && point.Completed == 0);
    }

    [Theory]
    [InlineData(DashboardTimeframe.Week, 2026, 9, 14)]
    [InlineData(DashboardTimeframe.Month, 2026, 9, 1)]
    [InlineData(DashboardTimeframe.SixMonths, 2026, 4, 1)]
    [InlineData(DashboardTimeframe.Year, 2026, 1, 1)]
    public async Task GetVelocityAsync_Timeframe_ExcludesBeforeStartAndFutureAndIncludesExactStart(
        DashboardTimeframe timeframe, int year, int month, int day)
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        var start = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        var asOf = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);
        var exactStart = SeedProjectTask(context, userId, ProjectType.Personal);
        exactStart.CreatedAt = start;
        var before = SeedProjectTask(context, userId, ProjectType.Personal);
        before.CreatedAt = start.AddTicks(-1);
        var future = SeedProjectTask(context, userId, ProjectType.Personal);
        future.CreatedAt = asOf.AddTicks(1);
        context.TaskActivityLogs.AddRange(
            new TaskActivityLog { TaskId = exactStart.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = start },
            new TaskActivityLog { TaskId = before.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = start.AddTicks(-1) },
            new TaskActivityLog { TaskId = future.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = asOf.AddTicks(1) });
        await context.SaveChangesAsync();

        var points = await new DashboardRepository(context).GetVelocityAsync(
            new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal }, timeframe, asOf);

        points.Sum(point => point.Created).Should().Be(1);
        points.Sum(point => point.Completed).Should().Be(1);
        points[0].Created.Should().Be(1);
        points[0].Completed.Should().Be(1);
    }

    [Fact]
    public async Task GetVelocityAsync_RecompletedTaskCountsOncePerBucket()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        var task = SeedProjectTask(context, userId, ProjectType.Personal, status: TaskItemStatus.Done);
        task.CreatedAt = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
        context.TaskActivityLogs.AddRange(
            new TaskActivityLog { TaskId = task.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc) },
            new TaskActivityLog { TaskId = task.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Done", NewValue = "InProgress", CreatedAt = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc) },
            new TaskActivityLog { TaskId = task.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "InProgress", NewValue = "Done", CreatedAt = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc) });
        await context.SaveChangesAsync();

        var points = await new DashboardRepository(context).GetVelocityAsync(
            new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal },
            DashboardTimeframe.Week, new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc));

        points[1].Completed.Should().Be(1);
        points.Sum(point => point.Completed).Should().Be(1);
    }

    [Fact]
    public async Task GetVelocityAsync_RecompletedTaskCanCountInSeparateBuckets()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        var task = SeedProjectTask(context, userId, ProjectType.Personal, status: TaskItemStatus.Done);
        task.CreatedAt = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
        context.TaskActivityLogs.AddRange(
            new TaskActivityLog { TaskId = task.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = new DateTime(2026, 9, 14, 23, 59, 59, DateTimeKind.Utc) },
            new TaskActivityLog { TaskId = task.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Done", NewValue = "InProgress", CreatedAt = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc) },
            new TaskActivityLog { TaskId = task.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "InProgress", NewValue = "Done", CreatedAt = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc) });
        await context.SaveChangesAsync();

        var points = await new DashboardRepository(context).GetVelocityAsync(
            new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal },
            DashboardTimeframe.Week, new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc));

        points[0].Completed.Should().Be(1);
        points[1].Completed.Should().Be(1);
        points.Sum(point => point.Completed).Should().Be(2);
    }

    [Fact]
    public async Task GetVelocityAsync_LegacyDoneTaskWithoutStatusLogHasNoHistoricalCompletion()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        var task = SeedProjectTask(context, userId, ProjectType.Personal, status: TaskItemStatus.Done);
        task.CreatedAt = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc);
        task.UpdatedAt = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync();

        var points = await new DashboardRepository(context).GetVelocityAsync(
            new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal },
            DashboardTimeframe.Week, new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc));

        points.Sum(point => point.Created).Should().Be(1);
        points.Sum(point => point.Completed).Should().Be(0);
    }

    [Theory]
    [InlineData(2024, 29)]
    [InlineData(2025, 28)]
    public async Task GetVelocityAsync_FebruaryMonth_UsesCalendarDayCount(int year, int expectedDays)
    {
        await using var context = CreateContext();
        var points = await new DashboardRepository(context).GetVelocityAsync(
            new DashboardScopeCriteria { UserId = Guid.NewGuid(), Scope = DashboardScope.Personal },
            DashboardTimeframe.Month, new DateTime(year, 2, 10, 12, 0, 0, DateTimeKind.Utc));

        points.Should().HaveCount(expectedDays);
        points[^1].End.Should().Be(new DateTime(year, 3, 1, 0, 0, 0, DateTimeKind.Utc));
    }


    [Fact]
    public async Task GetVelocityAsync_PersonalExcludesOtherPersonalAndOwnedOrAssignedTeamTasks()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, userId);
        SeedUser(context, otherId);
        SeedTeam(context, teamId, userId);
        var personal = SeedProjectTask(context, userId, ProjectType.Personal);
        var otherPersonal = SeedProjectTask(context, otherId, ProjectType.Personal);
        var ownedTeam = SeedProjectTask(context, userId, ProjectType.Team, workspaceId: teamId);
        var assignedTeam = SeedProjectTask(context, otherId, ProjectType.Team, workspaceId: teamId, assignedToId: userId);
        var createdAt = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc);
        foreach (var task in new[] { personal, otherPersonal, ownedTeam, assignedTeam })
        {
            task.CreatedAt = createdAt;
            context.TaskActivityLogs.Add(new TaskActivityLog
            {
                TaskId = task.Id, UserId = userId, Action = ActivityLogAction.StatusChanged,
                OldValue = "Todo", NewValue = "Done", CreatedAt = createdAt.AddHours(1)
            });
        }
        await context.SaveChangesAsync();

        var points = await new DashboardRepository(context).GetVelocityAsync(
            new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal },
            DashboardTimeframe.Week, new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc));

        points.Sum(point => point.Created).Should().Be(1);
        points.Sum(point => point.Completed).Should().Be(1);
    }


    [Fact]
    public async Task GetAnalyticsSnapshotAsync_PersonalScope_FiltersTasksCompletionsAndTimeEntries()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        SeedUser(context, userId);
        SeedUser(context, otherId);
        var ownTask = SeedProjectTask(context, userId, ProjectType.Personal, status: TaskItemStatus.Done);
        var otherTask = SeedProjectTask(context, otherId, ProjectType.Personal, status: TaskItemStatus.Done);
        var now = DateTime.UtcNow;
        context.TaskActivityLogs.AddRange(
            new TaskActivityLog { TaskId = ownTask.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = now.AddDays(-1) },
            new TaskActivityLog { TaskId = otherTask.Id, UserId = otherId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = now.AddDays(-1) });
        context.TimeEntries.AddRange(
            new TimeEntry { TaskId = ownTask.Id, UserId = userId, StartTime = now.AddDays(-1), EndTime = now.AddDays(-1).AddMinutes(30), DurationSeconds = 1800 },
            new TimeEntry { TaskId = otherTask.Id, UserId = otherId, StartTime = now.AddDays(-1), EndTime = now.AddDays(-1).AddHours(1), DurationSeconds = 3600 });
        await context.SaveChangesAsync();

        var snapshot = await new DashboardRepository(context).GetAnalyticsSnapshotAsync(
            new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal },
            null, now.AddDays(-7), now);

        snapshot.Tasks.Select(task => task.Id).Should().ContainSingle().Which.Should().Be(ownTask.Id);
        snapshot.Completions.Select(completion => completion.TaskId).Should().ContainSingle().Which.Should().Be(ownTask.Id);
        snapshot.TimeTrackedSeconds.Should().Be(1800);
    }

    [Fact]
    public async Task GetVelocityAsync_PersonalScope_CountsCreatedAndCompletionEventsInSixCalendarMonths()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        SeedUser(context, userId);
        SeedUser(context, otherId);
        var currentMonth = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var asOf = currentMonth.AddDays(15);
        var inScope = SeedProjectTask(context, userId, ProjectType.Personal, status: TaskItemStatus.Done);
        inScope.CreatedAt = currentMonth.AddMonths(-1).AddDays(2);
        inScope.UpdatedAt = currentMonth.AddDays(3);
        var outOfScope = SeedProjectTask(context, otherId, ProjectType.Personal, status: TaskItemStatus.Done);
        outOfScope.CreatedAt = currentMonth.AddMonths(-1).AddDays(3);
        context.TaskActivityLogs.AddRange(
            new TaskActivityLog { TaskId = inScope.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done", CreatedAt = currentMonth.AddDays(1) },
            new TaskActivityLog { TaskId = inScope.Id, UserId = userId, Action = ActivityLogAction.Updated, NewValue = "Done", CreatedAt = currentMonth.AddDays(2) },
            new TaskActivityLog { TaskId = outOfScope.Id, UserId = otherId, Action = ActivityLogAction.StatusChanged, NewValue = "Done", CreatedAt = currentMonth.AddDays(1) });
        await context.SaveChangesAsync();

        var points = await new DashboardRepository(context).GetVelocityAsync(new DashboardScopeCriteria
        {
            UserId = userId,
            Scope = DashboardScope.Personal
        }, DashboardTimeframe.SixMonths, asOf);

        points.Should().HaveCount(6);
        points.Select(point => point.Start).Should().BeInAscendingOrder();
        points[^2].Created.Should().Be(1);
        points[^2].Completed.Should().Be(0);
        points[^1].Created.Should().Be(0);
        points[^1].Completed.Should().Be(1);
        points.Take(4).Should().OnlyContain(point => point.Created == 0 && point.Completed == 0);
    }

    [Fact]
    public async Task GetVelocityAsync_TeamScope_ExcludesOtherTeamsAndDeletedTasks()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var teamAId = Guid.NewGuid();
        var teamBId = Guid.NewGuid();
        SeedUser(context, userId);
        SeedTeam(context, teamAId, userId);
        SeedTeam(context, teamBId, userId);
        var included = SeedProjectTask(context, userId, ProjectType.Team, workspaceId: teamAId);
        var otherTeam = SeedProjectTask(context, userId, ProjectType.Team, workspaceId: teamBId);
        var deleted = SeedProjectTask(context, userId, ProjectType.Team, workspaceId: teamAId);
        deleted.IsDeleted = true;
        var thisMonth = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        included.CreatedAt = thisMonth.AddDays(1);
        otherTeam.CreatedAt = thisMonth.AddDays(1);
        deleted.CreatedAt = thisMonth.AddDays(1);
        await context.SaveChangesAsync();

        var points = await new DashboardRepository(context).GetVelocityAsync(new DashboardScopeCriteria
        {
            UserId = userId,
            Scope = DashboardScope.Team,
            TeamId = teamAId
        }, DashboardTimeframe.SixMonths, thisMonth.AddDays(15));

        points[^1].Created.Should().Be(1);
        points[^1].Completed.Should().Be(0);
    }

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
