using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using Xunit;

namespace TaskHub.Tests.Integration;

public class DashboardAnalyticsEndpointTests
{
    [Fact]
    public async Task ActivityAndUpcomingEndpoints_EnforcePersonalScopeAndRejectUnauthorizedTeam()
    {
        await using var factory = new DashboardApiFactory();
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        Guid ownProjectId = Guid.Empty;
        Guid otherProjectId = Guid.Empty;
        using var client = factory.CreateClient();
        await SeedAsync(factory, context =>
        {
            SeedUser(context, user);
            SeedUser(context, other);
            var own = SeedTask(context, user);
            var otherTask = SeedTask(context, other);
            ownProjectId = own.List.Board.ProjectId!.Value;
            otherProjectId = otherTask.List.Board.ProjectId!.Value;
            own.DueDate = DateTime.UtcNow.Date;
            otherTask.DueDate = DateTime.UtcNow.Date;
            context.TaskActivityLogs.AddRange(
                new TaskActivityLog { TaskId = own.Id, UserId = user, Action = ActivityLogAction.Created },
                new TaskActivityLog { TaskId = otherTask.Id, UserId = other, Action = ActivityLogAction.Created });
            context.Teams.Add(new Team { Id = teamId, Name = "Private Team", CreatedById = other });
            context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = other, Role = TeamRole.Owner });
        });
        client.DefaultRequestHeaders.Add("X-Test-User", user.ToString());

        using var activity = await client.GetAsync("/api/v1/dashboard/activity?scope=Personal&page=1&pageSize=10");
        using var upcoming = await client.GetAsync("/api/v1/dashboard/upcoming?scope=Personal&page=1&pageSize=10");
        using var forbidden = await client.GetAsync($"/api/v1/dashboard/activity?scope=Team&teamId={teamId}");
        using var missingTeam = await client.GetAsync("/api/v1/dashboard/upcoming?scope=Team");
        using var ownProjectActivity = await client.GetAsync($"/api/v1/projects/{ownProjectId}/activity");
        using var otherProjectActivity = await client.GetAsync($"/api/v1/projects/{otherProjectId}/activity");

        Assert.Equal(HttpStatusCode.OK, activity.StatusCode);
        Assert.Equal(HttpStatusCode.OK, upcoming.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingTeam.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownProjectActivity.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherProjectActivity.StatusCode);
        using var activityJson = JsonDocument.Parse(await activity.Content.ReadAsStringAsync());
        using var upcomingJson = JsonDocument.Parse(await upcoming.Content.ReadAsStringAsync());
        Assert.Equal(1, activityJson.RootElement.GetProperty("data").GetProperty("totalItems").GetInt32());
        Assert.Equal(1, upcomingJson.RootElement.GetProperty("data").GetProperty("totalItems").GetInt32());
        using var ownProjectJson = JsonDocument.Parse(await ownProjectActivity.Content.ReadAsStringAsync());
        Assert.Equal(1, ownProjectJson.RootElement.GetProperty("data").GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task ConversionEndpoint_MovesProjectBetweenDashboardScopes()
    {
        await using var factory = new DashboardApiFactory();
        var owner = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        Guid projectId = Guid.Empty;
        using var client = factory.CreateClient();
        await SeedAsync(factory, context =>
        {
            SeedUser(context, owner);
            var task = SeedTask(context, owner);
            projectId = task.List.Board.ProjectId!.Value;
            context.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedById = owner });
            context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = owner, Role = TeamRole.Owner });
        });
        client.DefaultRequestHeaders.Add("X-Test-User", owner.ToString());

        using var conversion = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/convert-to-team", new { teamId });
        using var personal = await client.GetAsync("/api/v1/dashboard/stats?scope=Personal");
        using var team = await client.GetAsync($"/api/v1/dashboard/stats?scope=Team&teamId={teamId}");

        Assert.Equal(HttpStatusCode.OK, conversion.StatusCode);
        using var personalJson = JsonDocument.Parse(await personal.Content.ReadAsStringAsync());
        using var teamJson = JsonDocument.Parse(await team.Content.ReadAsStringAsync());
        Assert.Equal(0, personalJson.RootElement.GetProperty("data").GetProperty("total").GetInt32());
        Assert.Equal(1, teamJson.RootElement.GetProperty("data").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task PersonalProjectBoardTaskAndAssignmentEndpoints_EnforceOwnerBoundary()
    {
        await using var factory = new DashboardApiFactory();
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        Guid projectId = Guid.Empty;
        Guid boardId = Guid.Empty;
        Guid taskId = Guid.Empty;
        using var client = factory.CreateClient();
        await SeedAsync(factory, context =>
        {
            SeedUser(context, owner);
            SeedUser(context, outsider);
            var task = SeedTask(context, owner);
            projectId = task.List.Board.ProjectId!.Value;
            boardId = task.List.BoardId;
            taskId = task.Id;
        });

        client.DefaultRequestHeaders.Add("X-Test-User", outsider.ToString());
        using var projectDenied = await client.GetAsync($"/api/projects/{projectId}");
        using var boardDenied = await client.GetAsync($"/api/boards/{boardId}");
        using var taskDenied = await client.GetAsync($"/api/tasks/{taskId}");
        using var assignmentDenied = await client.PatchAsJsonAsync($"/api/tasks/{taskId}/assign", new { assignedToUserId = outsider });

        Assert.Equal(HttpStatusCode.Forbidden, projectDenied.StatusCode);
        Assert.True(boardDenied.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.Forbidden, taskDenied.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, assignmentDenied.StatusCode);

        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", owner.ToString());
        using var projectAllowed = await client.GetAsync($"/api/projects/{projectId}");
        using var boardAllowed = await client.GetAsync($"/api/boards/{boardId}");
        using var taskAllowed = await client.GetAsync($"/api/tasks/{taskId}");
        using var assignmentRejected = await client.PatchAsJsonAsync($"/api/tasks/{taskId}/assign", new { assignedToUserId = outsider });

        Assert.Equal(HttpStatusCode.OK, projectAllowed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, boardAllowed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, taskAllowed.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, assignmentRejected.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await context.TaskActivityLogs.AnyAsync(log => log.TaskId == taskId));
    }

    [Fact]
    public async Task ProjectActivityEndpoint_AllowsProjectGuestWithoutGrantingTeamDashboard()
    {
        await using var factory = new DashboardApiFactory();
        var owner = Guid.NewGuid();
        var guest = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        using var client = factory.CreateClient();
        await SeedAsync(factory, context =>
        {
            SeedUser(context, owner);
            SeedUser(context, guest);
            SeedUser(context, outsider);
            context.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedById = owner });
            context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = owner, Role = TeamRole.Owner });
            var project = new Project { Id = projectId, Name = "Team project", Slug = "team-project", OwnerId = owner, ProjectType = ProjectType.Team, WorkspaceId = teamId };
            context.Projects.Add(project);
            context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = guest, Role = ProjectRole.Guest });
            context.ProjectActivityLogs.Add(new ProjectActivityLog { ProjectId = projectId, UserId = owner, Action = ProjectActivityAction.Created });
        });

        client.DefaultRequestHeaders.Add("X-Test-User", guest.ToString());
        using var projectActivity = await client.GetAsync($"/api/v1/projects/{projectId}/activity");
        using var teamDashboard = await client.GetAsync($"/api/v1/dashboard/activity?scope=Team&teamId={teamId}");
        Assert.Equal(HttpStatusCode.OK, projectActivity.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, teamDashboard.StatusCode);
        using var activityJson = JsonDocument.Parse(await projectActivity.Content.ReadAsStringAsync());
        Assert.Equal(1, activityJson.RootElement.GetProperty("data").GetProperty("totalItems").GetInt32());

        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", outsider.ToString());
        using var denied = await client.GetAsync($"/api/v1/projects/{projectId}/activity");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task VelocityEndpoint_RequiresAuthentication()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/dashboard/velocity?timeframe=Week");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VelocityEndpoint_ReturnsOnlyAuthorizedPersonalProjectEvents()
    {
        await using var factory = new DashboardApiFactory();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        using var client = factory.CreateClient();
        await SeedAsync(factory, context =>
        {
            SeedUser(context, userId);
            SeedUser(context, otherId);
            var own = SeedTask(context, userId);
            var other = SeedTask(context, otherId);
            context.TaskActivityLogs.AddRange(
                new TaskActivityLog { TaskId = own.Id, UserId = userId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done" },
                new TaskActivityLog { TaskId = other.Id, UserId = otherId, Action = ActivityLogAction.StatusChanged, OldValue = "Todo", NewValue = "Done" });
        });
        client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());

        using var response = await client.GetAsync("/api/v1/dashboard/velocity?scope=Personal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var points = json.RootElement.GetProperty("data");
        Assert.Equal(6, points.GetArrayLength());
        Assert.Equal(1, points.EnumerateArray().Sum(point => point.GetProperty("created").GetInt32()));
        Assert.Equal(1, points.EnumerateArray().Sum(point => point.GetProperty("completed").GetInt32()));
    }

    [Fact]
    public async Task VelocityEndpoint_RejectsUserOutsideRequestedTeam()
    {
        await using var factory = new DashboardApiFactory();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        using var client = factory.CreateClient();
        await SeedAsync(factory, context =>
        {
            SeedUser(context, ownerId);
            SeedUser(context, outsiderId);
            context.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedById = ownerId });
            context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = ownerId, Role = TeamRole.Owner });
        });
        client.DefaultRequestHeaders.Add("X-Test-User", outsiderId.ToString());

        using var response = await client.GetAsync($"/api/v1/dashboard/velocity?scope=Team&teamId={teamId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VelocityEndpoint_RejectsTeamScopeWithoutTeamId()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());

        using var response = await client.GetAsync("/api/v1/dashboard/velocity?scope=Team&timeframe=Month");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task SeedAsync(DashboardApiFactory factory, Action<AppDbContext> seed)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(context);
        await context.SaveChangesAsync();
    }

    private static void SeedUser(AppDbContext context, Guid id)
    {
        context.Users.Add(new AppUser { Id = id, Email = $"{id:N}@example.com", FullName = "User", PasswordHash = "hash" });
    }

    private static TaskItem SeedTask(AppDbContext context, Guid ownerId)
    {
        var project = new Project { Name = "Project", Slug = $"p-{Guid.NewGuid():N}", OwnerId = ownerId, ProjectType = ProjectType.Personal };
        var board = new Board { Name = "Board", OwnerId = ownerId, ProjectId = project.Id };
        var list = new BoardList { Name = "List", BoardId = board.Id };
        var task = new TaskItem { Title = "Task", OwnerId = ownerId, ListId = list.Id, CreatedAt = DateTime.UtcNow };
        context.Projects.Add(project);
        context.Boards.Add(board);
        context.Lists.Add(list);
        context.Tasks.Add(task);
        return task;
    }

    private sealed class DashboardApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?> { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Guid.TryParse(Request.Headers["X-Test-User"], out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
