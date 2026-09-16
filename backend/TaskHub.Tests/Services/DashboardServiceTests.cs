using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetVelocityAsync_TeamScopeWithoutTeamId_IsRejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var act = () => service.GetVelocityAsync(Guid.NewGuid(), new DashboardQueryDto { Scope = "Team" });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetVelocityAsync_InvalidTimeframe_IsRejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var act = () => service.GetVelocityAsync(Guid.NewGuid(), new DashboardQueryDto { Timeframe = "Quarter" });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetVelocityAsync_UserWithoutTeamAccess_IsRejected()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, outsiderId);
        context.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedById = ownerId });
        context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = ownerId, Role = TeamRole.Owner });
        await context.SaveChangesAsync();

        var act = () => CreateService(context).GetVelocityAsync(outsiderId, new DashboardQueryDto
        {
            Scope = "Team",
            TeamId = teamId
        });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetStatsAsync_TeamScopeWithoutTeamId_IsRejected()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.GetStatsAsync(userId, new DashboardQueryDto { Scope = "Team" });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetStatsAsync_PersonalScopeWithTeamId_IsRejected()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.GetStatsAsync(userId, new DashboardQueryDto
        {
            Scope = "Personal",
            TeamId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetStatsAsync_InvalidScope_IsRejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var act = () => service.GetStatsAsync(Guid.NewGuid(), new DashboardQueryDto { Scope = "Everything" });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetVelocityAsync_NumericScope_IsRejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var act = () => service.GetVelocityAsync(Guid.NewGuid(), new DashboardQueryDto { Scope = "1", TeamId = Guid.NewGuid() });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetStatsAsync_UserWithoutTeamAccess_IsRejected()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, outsiderId);
        context.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedById = ownerId });
        context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = ownerId, Role = TeamRole.Owner });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.GetStatsAsync(outsiderId, new DashboardQueryDto
        {
            Scope = "Team",
            TeamId = teamId
        });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static DashboardService CreateService(AppDbContext context)
    {
        return new DashboardService(
            context,
            new DashboardRepository(context),
            new TeamRepository(context),
            new PermissionService(new ProjectRepository(context), new TeamRepository(context), new UserRepository(context)));
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
