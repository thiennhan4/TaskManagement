using FluentAssertions;
using NSubstitute;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using Xunit;

namespace TaskHub.Tests.Services;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task GetOverviewAsync_UsesAuthorizedScopeAndCompletionEvents()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scope = new DashboardQueryDto();
        var criteria = new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal };
        var dashboard = Substitute.For<IDashboardService>();
        var repository = Substitute.For<IDashboardRepository>();
        dashboard.ResolveScopeAsync(userId, scope, Arg.Any<CancellationToken>()).Returns(criteria);
        dashboard.GetVelocityAsync(userId, scope, Arg.Any<CancellationToken>()).Returns(new List<DashboardVelocityPointDto>
        {
            new() { Start = DateTime.UtcNow.Date, End = DateTime.UtcNow.Date.AddDays(1), Created = 2, Completed = 1 }
        });
        repository.GetAnalyticsSnapshotAsync(criteria, null, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardAnalyticsSnapshotDto
            {
                Tasks = new List<DashboardAnalyticsTaskDto>
                {
                    new() { Id = taskId, Status = TaskItemStatus.Done, Priority = TaskItemPriority.High, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new() { Id = Guid.NewGuid(), Status = TaskItemStatus.Todo, Priority = TaskItemPriority.Low, CreatedAt = DateTime.UtcNow.AddDays(-1) }
                },
                Completions = new List<DashboardCompletionDto>
                {
                    new() { TaskId = taskId, CompletedAt = DateTime.UtcNow.AddDays(-1) }
                },
                TimeTrackedSeconds = 1800
            });
        var service = new AnalyticsService(dashboard, repository, Substitute.For<IBoardRepository>(), Substitute.For<IPermissionService>());

        var result = await service.GetOverviewAsync(userId, scope, days: 7);

        result.TotalTasks.Should().Be(2);
        result.CompletedTasks.Should().Be(1);
        result.CompletionRate.Should().Be(50);
        result.Velocity.Sum(point => point.Completed).Should().Be(1);
        result.TotalTimeTrackedSeconds.Should().Be(1800);
        await repository.Received(1).GetAnalyticsSnapshotAsync(criteria, null, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task GetOverviewAsync_InvalidDays_IsRejected(int days)
    {
        var service = new AnalyticsService(
            Substitute.For<IDashboardService>(),
            Substitute.For<IDashboardRepository>(),
            Substitute.For<IBoardRepository>(),
            Substitute.For<IPermissionService>());

        var act = () => service.GetOverviewAsync(Guid.NewGuid(), new DashboardQueryDto(), days: days);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetOverviewAsync_EmptyScope_ReturnsZeroMetrics()
    {
        var userId = Guid.NewGuid();
        var scope = new DashboardQueryDto();
        var criteria = new DashboardScopeCriteria { UserId = userId, Scope = DashboardScope.Personal };
        var dashboard = Substitute.For<IDashboardService>();
        var repository = Substitute.For<IDashboardRepository>();
        dashboard.ResolveScopeAsync(userId, scope, Arg.Any<CancellationToken>()).Returns(criteria);
        dashboard.GetVelocityAsync(userId, scope, Arg.Any<CancellationToken>()).Returns(new List<DashboardVelocityPointDto>
        {
            new() { Start = DateTime.UtcNow.Date, End = DateTime.UtcNow.Date.AddDays(1) }
        });
        repository.GetAnalyticsSnapshotAsync(criteria, null, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardAnalyticsSnapshotDto());
        var service = new AnalyticsService(dashboard, repository, Substitute.For<IBoardRepository>(), Substitute.For<IPermissionService>());

        var result = await service.GetOverviewAsync(userId, scope, days: 7);

        result.TotalTasks.Should().Be(0);
        result.CompletionRate.Should().Be(0);
        result.AvgCompletionDays.Should().Be(0);
        result.Velocity.Should().ContainSingle(point => point.Created == 0 && point.Completed == 0);
    }
}
