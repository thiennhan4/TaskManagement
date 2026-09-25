using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Infrastructure.Data;
using TaskHub.Tests.Fixtures;
using Xunit;
using TaskHub.Domain.Entities;

namespace TaskHub.Tests.Integration;
public class Phase1SecurityEndpointTests
{
    [Fact]
    public async Task BoardAndListCreate_ReturnSafeDtos_WithEmptyCollections()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        using var created = await client.PostAsJsonAsync("/api/boards", new { name = "New board", color = "blue" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var boardJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        AssertSafe(boardJson.RootElement);
        var board = boardJson.RootElement.GetProperty("data");
        AssertFields(board, "id", "name", "color", "ownerId", "projectId", "createdAt", "updatedAt", "lists");
        Assert.Equal("New board", board.GetProperty("name").GetString());
        Assert.Equal(seed.Owner, board.GetProperty("ownerId").GetGuid());
        Assert.Equal(0, board.GetProperty("lists").GetArrayLength());
        using var detail = await client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        AssertSafe(detailJson.RootElement);

        // Project board authorization loads an existing graph before list creation.
        using var columnResponse = await client.PostAsJsonAsync("/api/boardlists",
            new { boardId = seed.Board, name = "New column", position = 3, color = "blue" });
        Assert.Equal(HttpStatusCode.OK, columnResponse.StatusCode);
        using var columnJson = JsonDocument.Parse(await columnResponse.Content.ReadAsStringAsync());
        AssertSafe(columnJson.RootElement);
        var column = columnJson.RootElement.GetProperty("data");
        AssertFields(column, "id", "name", "boardId", "color", "position", "createdAt", "updatedAt", "tasks");
        Assert.Equal(seed.Board, column.GetProperty("boardId").GetGuid());
        Assert.Equal(3, column.GetProperty("position").GetInt32());
        Assert.Equal(0, column.GetProperty("tasks").GetArrayLength());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoardAndListDetails_PreserveTaskDisplayFieldsAndCounts(bool listsEndpoint)
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var task = await db.Tasks.SingleAsync(t => t.Id == seed.Task);
            task.AssignedToId = seed.Owner;
            task.Description = "Description";
            task.Label = "Label";
            task.Progress = 25;
            db.Comments.Add(new Comment { TaskId = task.Id, UserId = seed.Owner, Content = "Visible comment" });
            db.Comments.Add(new Comment { TaskId = task.Id, UserId = seed.Owner, Content = "Deleted comment", IsDeleted = true });
            db.TaskAttachments.Add(new TaskAttachment { TaskId = task.Id, UploadedByUserId = seed.Owner,
                FileName = "test.txt", FilePath = "SYNTHETIC_PRIVATE_PATH", ContentType = "text/plain" });
            await db.SaveChangesAsync();
        }
        using var client = factory.Client(seed.Owner);
        var path = listsEndpoint ? $"/api/boardlists/board/{seed.Board}" : $"/api/boards/{seed.Board}";
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SYNTHETIC_PRIVATE_PATH", body);
        using var json = JsonDocument.Parse(body);
        AssertSafe(json.RootElement);
        var data = json.RootElement.GetProperty("data");
        if (!listsEndpoint) Assert.Equal(seed.Project, data.GetProperty("projectId").GetGuid());
        var lists = listsEndpoint ? data : data.GetProperty("lists");
        var tasks = lists[0].GetProperty("tasks");
        Assert.Equal(1, tasks.GetArrayLength());
        var card = tasks[0];
        Assert.Equal(seed.Task, card.GetProperty("id").GetGuid());
        Assert.Equal(seed.List, card.GetProperty("listId").GetGuid());
        Assert.Equal(seed.Board, card.GetProperty("boardId").GetGuid());
        Assert.Equal("Todo", card.GetProperty("listName").GetString());
        Assert.Equal("Main", card.GetProperty("boardName").GetString());
        Assert.Equal("Description", card.GetProperty("description").GetString());
        Assert.Equal("Label", card.GetProperty("label").GetString());
        Assert.Equal(25, card.GetProperty("progress").GetInt32());
        Assert.Equal("Owner", card.GetProperty("ownerName").GetString());
        Assert.Equal("Owner", card.GetProperty("assignedToName").GetString());
        Assert.Equal(1, card.GetProperty("commentsCount").GetInt32());
        Assert.Equal(1, card.GetProperty("attachmentsCount").GetInt32());
        Assert.Equal(JsonValueKind.String, card.GetProperty("status").ValueKind);
        Assert.Equal(JsonValueKind.String, card.GetProperty("priority").ValueKind);
    }

    private static void AssertFields(JsonElement value, params string[] allowed) =>
        Assert.All(value.EnumerateObject(), property => Assert.Contains(property.Name, allowed));

    [Theory]
    [InlineData(null)]
    [InlineData("https://example.invalid/avatar.png")]
    public async Task Activity_PreservesDistinctActor_AndNullableAvatar_WithoutAccountFields(string? avatar)
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var actor = await db.Users.SingleAsync(u => u.Id == seed.Outsider);
            actor.AvatarUrl = avatar;
            var log = await db.ProjectActivityLogs.SingleAsync(l => l.ProjectId == seed.Project);
            log.UserId = actor.Id;
            log.Metadata = "SYNTHETIC_PRIVATE_METADATA";
            await db.SaveChangesAsync();
            var relation = db.Model.FindEntityType(typeof(ProjectActivityLog))!.GetForeignKeys()
                .Single(f => f.Properties.Any(p => p.Name == nameof(ProjectActivityLog.UserId)));
            Assert.True(relation.IsRequired);
        }
        using var client = factory.Client(seed.Owner);
        using var response = await client.GetAsync($"/api/projects/{seed.Project}/activity");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SYNTHETIC_PRIVATE_METADATA", body);
        Assert.DoesNotContain("SYNTHETIC_HASH", body);
        using var json = JsonDocument.Parse(body);
        AssertSafe(json.RootElement);
        var activity = json.RootElement.GetProperty("data")[0];
        AssertFields(activity, "id", "projectId", "userId", "action", "description", "createdAt", "user");
        Assert.Equal(seed.Outsider, activity.GetProperty("userId").GetGuid());
        var user = activity.GetProperty("user");
        AssertFields(user, "id", "fullName", "avatarUrl");
        Assert.Equal(seed.Outsider, user.GetProperty("id").GetGuid());
        Assert.Equal("Outsider", user.GetProperty("fullName").GetString());
        if (avatar is null) Assert.False(user.TryGetProperty("avatarUrl", out _));
        else Assert.Equal(avatar, user.GetProperty("avatarUrl").GetString());
    }

    [Fact]
    public async Task BoardAndActivityResponses_ExcludeEntityGraphsAndSecrets()
    {
        await using var factory=new Phase1ApiFactory(); var seed=await factory.SeedAsync(); using var client=factory.Client(seed.Owner);
        foreach(var path in new[] { $"/api/projects/{seed.Project}/activity",$"/api/projects/{seed.Project}/boards","/api/boards",$"/api/boards/{seed.Board}",$"/api/boardlists/board/{seed.Board}" })
        {
            using var response=await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            var text=await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("SYNTHETIC_HASH",text);
            using var json=JsonDocument.Parse(text); AssertSafe(json.RootElement);
            Assert.DoesNotContain("Deleted task",text);
        }
    }
    [Fact]
    public async Task Kanban_ReloadsPersistedCards_AndRejectsWrongBoardAndOutsider()
    {
        await using var factory=new Phase1ApiFactory(); var seed=await factory.SeedAsync(); using var client=factory.Client(seed.Owner);
        for(var i=0;i<2;i++)
        {
            using var response=await client.GetAsync($"/api/v1/projects/{seed.Project}/kanban"); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var data=json.RootElement.GetProperty("data"); AssertSafe(data);
            Assert.Equal(seed.Board,data.GetProperty("board").GetProperty("id").GetGuid());
            var tasks=data.GetProperty("lists").GetProperty("items")[0].GetProperty("tasks");
            Assert.Equal(1,tasks.GetProperty("totalItems").GetInt32()); Assert.Equal(seed.Task,tasks.GetProperty("items")[0].GetProperty("id").GetGuid());
            Assert.True(data.GetProperty("canManageColumns").GetBoolean());
        }
        using var wrong=await client.GetAsync($"/api/v1/projects/{seed.Project}/kanban?boardId={Guid.NewGuid()}"); Assert.Equal(HttpStatusCode.NotFound,wrong.StatusCode);
        using var invalid=await client.GetAsync($"/api/v1/projects/{seed.Project}/kanban?taskPageSize=101"); Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
        using var outsider=factory.Client(seed.Outsider); using var denied=await outsider.GetAsync($"/api/v1/projects/{seed.Project}/kanban"); Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
    }
    [Fact]
    public async Task CommentsHaveOneRouteOwner_AndScopedDeleteRejectsWrongParent()
    {
        await using var factory=new Phase1ApiFactory(); var seed=await factory.SeedAsync(); using var client=factory.Client(seed.Owner);
        using var created=await client.PostAsJsonAsync($"/api/tasks/{seed.Task}/comments",new { content="Comment" }); Assert.Equal(HttpStatusCode.OK,created.StatusCode);
        using var json=JsonDocument.Parse(await created.Content.ReadAsStringAsync()); var id=json.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        using var listed=await client.GetAsync($"/api/tasks/{seed.Task}/comments"); Assert.Equal(HttpStatusCode.OK,listed.StatusCode);
        using var mismatch=await client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}/comments/{id}"); Assert.Equal(HttpStatusCode.NotFound,mismatch.StatusCode);
        using var deleted=await client.DeleteAsync($"/api/tasks/{seed.Task}/comments/{id}"); Assert.Equal(HttpStatusCode.OK,deleted.StatusCode);
    }
    [Fact]
    public async Task TimeHistoryAndWrites_RequireTaskAccess()
    {
        await using var factory=new Phase1ApiFactory(); var seed=await factory.SeedAsync(); using var client=factory.Client(seed.Outsider);
        using var history=await client.GetAsync($"/api/v1/timetracking/task/{seed.Task}"); Assert.Equal(HttpStatusCode.Forbidden,history.StatusCode);
        using var start=await client.PostAsJsonAsync("/api/timetracking/start",new { taskId=seed.Task }); Assert.Equal(HttpStatusCode.Forbidden,start.StatusCode);
        using var owner=factory.Client(seed.Owner); using var started=await owner.PostAsJsonAsync("/api/timetracking/start",new { taskId=seed.Task }); Assert.Equal(HttpStatusCode.OK,started.StatusCode);
        using var duplicate=await owner.PostAsJsonAsync("/api/timetracking/start",new { taskId=seed.Task }); Assert.Equal(HttpStatusCode.Conflict,duplicate.StatusCode);
    }
    [Theory]
    [InlineData("login")][InlineData("register")][InlineData("refresh")][InlineData("google")]
    public async Task EveryAuthRoute_HasEffectiveRateLimit(string route)
    {
        await using var factory=new Phase1ApiFactory(); using var client=factory.Client();
        for(var i=0;i<5;i++) { using var response=await client.PostAsJsonAsync($"/api/auth/{route}",new { }); Assert.NotEqual(HttpStatusCode.TooManyRequests,response.StatusCode); }
        using var rejected=await client.PostAsJsonAsync($"/api/auth/{route}",new { }); Assert.Equal(HttpStatusCode.TooManyRequests,rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
        using var json=JsonDocument.Parse(await rejected.Content.ReadAsStringAsync()); Assert.False(json.RootElement.GetProperty("success").GetBoolean());
    }
    [Fact]
    public async Task InactiveAccount_CannotRefresh_AndSessionsAreRevoked()
    {
        await using var factory=new Phase1ApiFactory(); var seed=await factory.SeedAsync(); string refresh;
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AppDbContext>(); var user=await db.Users.SingleAsync(u=>u.Id==seed.Owner); user.IsActive=false;
            var token=new TaskHub.Domain.Entities.RefreshToken { UserId=seed.Owner, Token=Guid.NewGuid().ToString("N"), ExpiresAt=DateTime.UtcNow.AddDays(1) }; db.RefreshTokens.Add(token); await db.SaveChangesAsync(); refresh=token.Token;
        }
        using var client=factory.Client(); using var request=new HttpRequestMessage(HttpMethod.Post,"/api/auth/refresh"); request.Headers.Add("Cookie",$"refreshToken={refresh}");
        using var response=await client.SendAsync(request); Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode); Assert.False(response.Headers.Contains("Set-Cookie"));
        using var check=factory.Services.CreateScope(); Assert.True(await check.ServiceProvider.GetRequiredService<AppDbContext>().RefreshTokens.AllAsync(t=>t.IsRevoked));
    }
    private static void AssertSafe(JsonElement value)
    {
        if(value.ValueKind==JsonValueKind.Object) foreach(var property in value.EnumerateObject())
        {
            Assert.DoesNotContain(property.Name,new[] { "passwordHash","refreshToken","refreshTokens","metadata","project","owner","assignedTo" }); AssertSafe(property.Value);
        }
        else if(value.ValueKind==JsonValueKind.Array) foreach(var item in value.EnumerateArray()) AssertSafe(item);
    }
}
