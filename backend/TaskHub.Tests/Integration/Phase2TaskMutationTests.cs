using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaskHub.Domain.Entities;
using TaskHub.Tests.Fixtures;
using Xunit;
namespace TaskHub.Tests.Integration;

public class Phase2TaskMutationTests
{
    [SqlFact]
    public async Task TitleOnlyEdit_PreservesOmittedFields_ExplicitNullClearsDates()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedTeamAsync();
        var start = DateTime.UtcNow.Date.AddDays(-3); var due = start.AddDays(1);
        await factory.WithDb(async db => { var task = await db.Tasks.SingleAsync(t => t.Id == seed.Resources.Task); task.StartDate = start; task.DueDate = due; task.Progress = 43; task.Status = TaskItemStatus.Review; task.Priority = TaskItemPriority.Critical; task.Description = "Preserve me"; await db.SaveChangesAsync(); });
        using var client = factory.Client(seed.Resources.Owner);
        Assert.True((await client.PutAsJsonAsync($"/api/tasks/{seed.Resources.Task}", new { title = "Renamed" })).IsSuccessStatusCode);
        await factory.WithDb(async db => { var task = await db.Tasks.SingleAsync(t => t.Id == seed.Resources.Task); Assert.Equal(start, task.StartDate); Assert.Equal(due, task.DueDate); Assert.Equal(43, task.Progress); Assert.Equal(seed.Member, task.AssignedToId); Assert.Equal(TaskItemStatus.Review, task.Status); Assert.Equal(TaskItemPriority.Critical, task.Priority); Assert.Equal("Preserve me", task.Description); });
        Assert.True((await client.PutAsJsonAsync($"/api/tasks/{seed.Resources.Task}", new { title = "Renamed", dueDate = (DateTime?)null, startDate = (DateTime?)null, assignedToId = (Guid?)null })).IsSuccessStatusCode);
        await factory.WithDb(async db => { var task = await db.Tasks.SingleAsync(t => t.Id == seed.Resources.Task); Assert.Null(task.DueDate); Assert.Null(task.StartDate); Assert.Null(task.AssignedToId); });
    }

    [SqlFact]
    public async Task InvalidMergedDatesAndEnumsRejected_OverdueCompletionAllowed()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        await factory.WithDb(async db => { var task = await db.Tasks.SingleAsync(t => t.Id == seed.Task); task.DueDate = DateTime.UtcNow.Date.AddDays(-1); await db.SaveChangesAsync(); });
        using var client = factory.Client(seed.Owner);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PutAsJsonAsync($"/api/tasks/{seed.Task}", new { title = "Invalid", startDate = DateTime.UtcNow })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/tasks/{seed.Task}/status", new { newStatus = 999 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/tasks/{seed.Task}", new { title = "Invalid", priority = 999 })).StatusCode);
        Assert.True((await client.PatchAsJsonAsync($"/api/tasks/{seed.Task}/status", new { newStatus = "Done" })).IsSuccessStatusCode);
    }

    [SqlFact]
    public async Task Move_ReordersSiblings_RejectsCrossBoardAndStaleRequest()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        Guid destination = Guid.NewGuid(), otherList = Guid.NewGuid();
        await factory.WithDb(async db =>
        {
            db.Lists.Add(new BoardList { Id = destination, BoardId = seed.Board, Name = "Done" });
            var otherBoard = new Board { Name = "Other", OwnerId = seed.Owner }; db.Boards.Add(otherBoard);
            db.Lists.Add(new BoardList { Id = otherList, BoardId = otherBoard.Id, Name = "Other" });
            db.Tasks.Add(new TaskItem { Title = "Sibling", ListId = destination, OwnerId = seed.Owner, Position = 0 });
            await db.SaveChangesAsync();
        });
        using var client = factory.Client(seed.Owner);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PatchAsJsonAsync($"/api/tasks/{seed.Task}/move", new { listId = otherList, position = 0 })).StatusCode);
        Assert.True((await client.PatchAsJsonAsync($"/api/tasks/{seed.Task}/move", new { listId = destination, position = 0, expectedUpdatedAt = (DateTime?)null })).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/tasks/{seed.Task}/move", new { listId = seed.List, position = 0, expectedUpdatedAt = (DateTime?)null })).StatusCode);
        await factory.WithDb(async db => { var tasks = await db.Tasks.Where(t => t.ListId == destination && !t.IsDeleted).OrderBy(t => t.Position).ToListAsync(); Assert.Equal(new[] { 0, 1 }, tasks.Select(t => t.Position)); Assert.Equal(seed.Task, tasks[0].Id); Assert.Equal(TaskItemStatus.Todo, tasks[0].Status); });
    }

    [SqlFact]
    public async Task Attachment_LaterActivityFailure_CompensatesWrittenFile()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        factory.Failure.EntityName = nameof(TaskActivityLog); using var client = factory.Client(seed.Owner);
        using var form = new MultipartFormDataContent(); form.Add(new StringContent("private test content"), "file", "test.txt");
        var response = await client.PostAsync($"/api/tasks/{seed.Task}/attachments", form);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDb(async db => Assert.Empty(await db.TaskAttachments.ToListAsync()));
        Assert.Empty(Directory.Exists(factory.AttachmentRoot) ? Directory.GetFiles(factory.AttachmentRoot) : Array.Empty<string>());
    }

    [SqlFact]
    public async Task Attachment_DeleteFailure_PreservesMetadataAndFile()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner); using var form = new MultipartFormDataContent(); form.Add(new StringContent("private test content"), "file", "test.txt");
        Assert.True((await client.PostAsync($"/api/tasks/{seed.Task}/attachments", form)).IsSuccessStatusCode);
        Guid id = default; await factory.WithDb(async db => id = (await db.TaskAttachments.SingleAsync()).Id);
        factory.Failure.EntityName = nameof(TaskActivityLog);
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.DeleteAsync($"/api/tasks/{seed.Task}/attachments/{id}")).StatusCode);
        await factory.WithDb(async db => Assert.Single(await db.TaskAttachments.ToListAsync())); Assert.Single(Directory.GetFiles(factory.AttachmentRoot));
    }

    [SqlFact]
    public async Task TeamTransfer_RacingMemberRemoval_CannotLoseLastOwner()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedTeamAsync();
        using var a = factory.Client(seed.Resources.Owner); using var b = factory.Client(seed.Resources.Owner);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transfer = Task.Run(async () => { await gate.Task; return await a.PostAsJsonAsync($"/api/v1/teams/{seed.Team}/transfer-ownership", new { targetUserId = seed.Member }); });
        var remove = Task.Run(async () => { await gate.Task; return await b.DeleteAsync($"/api/teams/{seed.Team}/members/{seed.Member}"); });
        gate.SetResult(); var results = await Task.WhenAll(transfer, remove);
        Assert.Contains(results, r => r.IsSuccessStatusCode);
        Assert.All(results, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity));
        foreach (var response in results.Where(r => r.StatusCode == HttpStatusCode.UnprocessableEntity))
            Assert.Contains("New owner must already be a team member", await response.Content.ReadAsStringAsync());
        await factory.WithDb(async db => Assert.True(await db.TeamMembers.AnyAsync(m => m.TeamId == seed.Team && m.Role == TeamRole.Owner)));
    }
}
