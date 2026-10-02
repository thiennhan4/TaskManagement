using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class Phase3ReadTests
{
    [Fact]
    public async Task BoardSearch_FiltersBeforePagingAndCountsAllMatches()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var i=0; i<60; i++)
                db.Tasks.Add(new TaskItem { ListId=seed.List, OwnerId=seed.Owner, Title="Ordinary", Position=i });
            db.Tasks.Add(new TaskItem { ListId=seed.List, OwnerId=seed.Owner, Title="Beyond first page", Description="Needle", Position=100 });
            await db.SaveChangesAsync();
        }
        using var client = factory.Client(seed.Owner);
        foreach (var (url, property) in new[] {
            ($"/api/boards/{seed.Board}", "listPage"),
            ($"/api/v1/projects/{seed.Project}/kanban", "lists") })
        {
            using var json = JsonDocument.Parse(await client.GetStringAsync(url+"?searchKeyword=Needle&taskPageSize=1"));
            var tasks = json.RootElement.GetProperty("data").GetProperty(property).GetProperty("items")[0].GetProperty("tasks");
            Assert.Equal(1,tasks.GetProperty("totalItems").GetInt32());
            Assert.Equal("Beyond first page",tasks.GetProperty("items")[0].GetProperty("title").GetString());
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(url+"?searchKeyword="+new string('a',201))).StatusCode);
        }
    }

    [Fact]
    public async Task LegacyCollections_PreserveArraysAndExposePagination()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        foreach (var url in new[] { "/api/boards", $"/api/projects/{seed.Project}/boards",
            $"/api/projects/{seed.Project}/members", $"/api/tasks/{seed.Task}/comments",
            $"/api/boardlists/board/{seed.Board}" })
        {
            using var response = await client.GetAsync(url + "?pageSize=1");
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("data").ValueKind);
            Assert.True(response.Headers.Contains("X-Pagination"), url);
            using var metadata = JsonDocument.Parse(response.Headers.GetValues("X-Pagination").Single());
            Assert.Equal(1, metadata.RootElement.GetProperty("PageSize").GetInt32());
            if (url.Contains("boardlists"))
                Assert.Equal(1, json.RootElement.GetProperty("data")[0].GetProperty("taskPage").GetProperty("totalItems").GetInt32());
        }
    }

    [Fact]
    public async Task TaskPages_FilterOrderCountAndRejectInvalidBounds()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 25; i++) db.Tasks.Add(new TaskItem { ListId=seed.List, OwnerId=seed.Owner, Title="Tie", CreatedAt=new DateTime(2026,1,1) });
            db.Comments.Add(new Comment { TaskId=seed.Task, UserId=seed.Owner, Content="Visible" });
            db.Comments.Add(new Comment { TaskId=seed.Task, UserId=seed.Owner, Content="Deleted", IsDeleted=true });
            db.TaskAttachments.Add(new TaskAttachment { TaskId=seed.Task, UploadedByUserId=seed.Owner, FileName="a.txt", FilePath="private", ContentType="text/plain" });
            await db.SaveChangesAsync();
        }
        using var client=factory.Client(seed.Owner);
        var seen=new HashSet<Guid>();
        for(var page=1; page<=3; page++)
        {
            using var json=JsonDocument.Parse(await client.GetStringAsync($"/api/v1/tasks/my-tasks?searchKeyword=Tie&page={page}&pageSize=10&sortBy=title"));
            var data=json.RootElement.GetProperty("data");
            Assert.Equal(25,data.GetProperty("totalItems").GetInt32());
            foreach(var row in data.GetProperty("items").EnumerateArray()) Assert.True(seen.Add(row.GetProperty("id").GetGuid()));
        }
        Assert.Equal(25,seen.Count);
        using var summary=JsonDocument.Parse(await client.GetStringAsync("/api/v1/tasks/summary"));
        Assert.Equal(26,summary.RootElement.GetProperty("data").GetProperty("total").GetInt32());
        foreach(var query in new[]{"page=0","pageSize=0","pageSize=101"})
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/v1/tasks?"+query)).StatusCode);
        using var update=await client.PutAsJsonAsync($"/api/tasks/{seed.Task}",new { title="Updated" });
        update.EnsureSuccessStatusCode();
        using var updated=JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        Assert.Equal(1,updated.RootElement.GetProperty("data").GetProperty("commentsCount").GetInt32());
        Assert.Equal(1,updated.RootElement.GetProperty("data").GetProperty("attachmentsCount").GetInt32());
        using var outsider=factory.Client(seed.Outsider);
        using var outside=JsonDocument.Parse(await outsider.GetStringAsync("/api/v1/tasks"));
        Assert.Equal(0,outside.RootElement.GetProperty("data").GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("2026-01-01","2026-03-05")]
    [InlineData("2026-02-01","2026-01-01")]
    public async Task CalendarRejectsInvalidWindows(string start,string end)
    {
        await using var factory=new Phase1ApiFactory();
        var seed=await factory.SeedAsync();
        using var client=factory.Client(seed.Owner);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync($"/api/v1/tasks/calendar?start={start}&end={end}")).StatusCode);
    }

    [Fact]
    public async Task CanonicalActivityHasOneHttpOwnerAndAllowsOnlyProjectAccess()
    {
        await using var factory=new Phase1ApiFactory();
        var seed=await factory.SeedAsync();
        using var client=factory.Client(seed.Owner);
        using var json=JsonDocument.Parse(await client.GetStringAsync($"/api/v1/projects/{seed.Project}/activity?pageSize=1"));
        Assert.Equal(1,json.RootElement.GetProperty("data").GetProperty("pageSize").GetInt32());
        using var outsider=factory.Client(seed.Outsider);
        Assert.Equal(HttpStatusCode.Forbidden,(await outsider.GetAsync($"/api/v1/projects/{seed.Project}/activity")).StatusCode);
    }
}
