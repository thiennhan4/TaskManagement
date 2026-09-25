using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Application.Hubs;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class RealtimeRevocationIntegrationTests
{
    [Fact]
    public async Task MembershipRevocation_ProtectsAllConnections_AndPreservesAlternateAccess()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        using var ownerHttp = factory.Client(seed.Resources.Owner);
        using var memberHttp = factory.Client(seed.Member);
        var token = memberHttp.DefaultRequestHeaders.Authorization!.Parameter!;
        HubConnection Connect() => new HubConnectionBuilder().WithUrl("https://localhost/hubs/notification", options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        }).Build();
        await using var first = Connect();
        await using var second = Connect();
        var inboxes = new[] { Channel.CreateUnbounded<string>(), Channel.CreateUnbounded<string>() };
        var connections = new[] { first, second };
        for (var i = 0; i < connections.Length; i++)
        {
            var inbox = inboxes[i];
            connections[i].On<string>("Probe", value => inbox.Writer.TryWrite(value));
            connections[i].On<string>("Barrier", value => inbox.Writer.TryWrite(value));
            await connections[i].StartAsync();
            await connections[i].InvokeAsync("JoinProject", seed.Resources.Project.ToString());
            await connections[i].InvokeAsync("JoinBoard", seed.Resources.Board.ToString());
            await connections[i].InvokeAsync("JoinTask", seed.Resources.Task.ToString());
            await connections[i].InvokeAsync("JoinTeam", seed.Team.ToString());
        }
        var dispatcher = factory.Services.GetRequiredService<IProtectedHubContext>();
        var raw = factory.Services.GetRequiredService<IHubContext<NotificationHub>>();
        var groups = new[] { $"project_{seed.Resources.Project}", $"board_{seed.Resources.Board}", $"task_{seed.Resources.Task}", $"team_{seed.Team}" };
        async Task AssertDelivery(bool allowed)
        {
            var marker = Guid.NewGuid().ToString();
            foreach (var group in groups) await dispatcher.Clients.Group(group).SendAsync("Probe", group);
            // Same transport queue: the barrier follows all completed protected sends.
            await raw.Clients.All.SendAsync("Barrier", marker);
            foreach (var inbox in inboxes)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var received = new List<string>();
                while (true)
                {
                    var value = await inbox.Reader.ReadAsync(timeout.Token);
                    if (value == marker) break;
                    received.Add(value);
                }
                Assert.Equal(allowed ? groups : Array.Empty<string>(), received);
            }
        }
        await AssertDelivery(true);
        using var projectRemoval = await ownerHttp.DeleteAsync($"/api/projects/{seed.Resources.Project}/members/{seed.Member}");
        projectRemoval.EnsureSuccessStatusCode();
        await AssertDelivery(true); // Workspace membership still grants access.
        using var teamRemoval = await ownerHttp.DeleteAsync($"/api/teams/{seed.Team}/members/{seed.Member}");
        teamRemoval.EnsureSuccessStatusCode();
        await AssertDelivery(false);
        await first.StopAsync();
        await first.StartAsync();
        await Assert.ThrowsAsync<HubException>(() => first.InvokeAsync("JoinTask", seed.Resources.Task.ToString()));
    }
}
