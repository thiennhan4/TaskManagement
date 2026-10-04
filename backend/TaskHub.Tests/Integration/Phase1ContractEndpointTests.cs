using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class Phase1ContractEndpointTests
{
    [Fact]
    public async Task AuthLimiter_IsClientPartitioned_AndCannotBeBypassedByPathCasing()
    {
        await using var factory = new Phase1ApiFactory();
        _ = factory.Services;
        async Task<HttpContext> Send(string ip, string path) => await factory.Server.SendAsync(ctx =>
        {
            ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
            ctx.Request.Scheme = "https";
            ctx.Request.Method = "POST";
            ctx.Request.Path = path;
            ctx.Request.ContentType = "application/json";
            ctx.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        });
        for (var i = 0; i < 5; i++) Assert.NotEqual(429, (await Send("192.0.2.1", "/api/auth/login")).Response.StatusCode);
        Assert.Equal(429, (await Send("192.0.2.1", "/API/AUTH/LOGIN/")).Response.StatusCode);
        Assert.NotEqual(429, (await Send("192.0.2.2", "/api/auth/login")).Response.StatusCode);
        Assert.NotEqual(429, (await Send("192.0.2.1", "/api/auth/register")).Response.StatusCode);
    }

    [Fact]
    public async Task AttributeRoutes_HaveOneOwnerPerVerbAndTemplate()
    {
        await using var factory = new Phase1ApiFactory();
        var actions = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>();
        var routes = actions.Where(a => a.AttributeRouteInfo != null).SelectMany(a =>
            (a.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods) ?? ["ANY"])
            .Select(verb => verb + ":" + System.Text.RegularExpressions.Regex.Replace(a.AttributeRouteInfo!.Template!, @"\{[^}]+\}", "{}").ToLowerInvariant()));
        Assert.DoesNotContain(routes.GroupBy(r => r), g => g.Count() > 1);
    }

    [Fact]
    public async Task CanonicalCollections_AreBoundedSafeAndAuthorized_WithStableCardPaging()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        var ids = new List<Guid>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 3; i++)
            {
                var task = new TaskItem { Title="Paged card", ListId=seed.Resources.List, OwnerId=seed.Resources.Owner, Position=0 };
                ids.Add(task.Id); db.Tasks.Add(task);
                db.Comments.Add(new Comment { TaskId=seed.Resources.Task, UserId=seed.Member, Content="Safe comment" });
            }
            await db.SaveChangesAsync();
        }
        using var owner = factory.Client(seed.Resources.Owner);
        using var outsider = factory.Client(seed.Resources.Outsider);
        foreach (var path in new[] { "/api/v1/boards", $"/api/v1/projects/{seed.Resources.Project}/boards",
            $"/api/v1/boardlists/board/{seed.Resources.Board}", $"/api/v1/tasks/{seed.Resources.Task}/comments", $"/api/v1/projects/{seed.Resources.Project}/members" })
        {
            using var response = await owner.GetAsync(path + "?pageSize=1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("passwordHash", text); Assert.DoesNotContain("refreshToken", text);
            using var json = JsonDocument.Parse(text);
            var page = json.RootElement.GetProperty("data");
            Assert.Equal(1, page.GetProperty("pageSize").GetInt32());
            Assert.Single(page.GetProperty("items").EnumerateArray());
            using var invalid = await owner.GetAsync(path + "?pageSize=101");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            if (path != "/api/v1/boards")
            {
                using var denied = await outsider.GetAsync(path);
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            }
        }
        var seen = new List<Guid>();
        for (var page = 1; page <= 4; page++)
        {
            using var response = await owner.GetAsync($"/api/v1/projects/{seed.Resources.Project}/kanban?taskPageSize=1&taskPage={page}");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var tasks = json.RootElement.GetProperty("data").GetProperty("lists").GetProperty("items")[0].GetProperty("tasks");
            Assert.Equal(4, tasks.GetProperty("totalItems").GetInt32());
            seen.Add(tasks.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        Assert.Equal(4, seen.Distinct().Count());
        Assert.Contains(seed.Resources.Task, seen);
        Assert.All(ids, id => Assert.Contains(id, seen));
    }
}
