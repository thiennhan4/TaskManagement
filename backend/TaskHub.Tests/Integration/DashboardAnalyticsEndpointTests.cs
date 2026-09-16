using System.Net;
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
