using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Storage;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class Phase2RecoveryTests
{
    [SqlFact]
    public async Task PersonalSlugLookup_SelectsCurrentOwnersScope()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        var other = new Project { Name = "Other", Slug = "private", OwnerId = seed.Outsider, ProjectType = ProjectType.Personal };
        await factory.WithDb(async db => { db.Projects.Add(other); await db.SaveChangesAsync(); });
        await using var scope = factory.Services.CreateAsyncScope();
        var projects = scope.ServiceProvider.GetRequiredService<IProjectService>();
        Assert.Equal(seed.Project, (await projects.GetProjectBySlugAsync("private", null, seed.Owner))!.Id);
        Assert.Equal(other.Id, (await projects.GetProjectBySlugAsync("private", null, seed.Outsider))!.Id);
    }

    [SqlFact]
    public async Task ActiveTimerConstraint_RejectsDirectDuplicate_AllowsStoppedHistory()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        await factory.WithDb(async db =>
        {
            db.TimeEntries.Add(new TimeEntry { TaskId = seed.Task, UserId = seed.Owner });
            db.TimeEntries.Add(new TimeEntry { TaskId = seed.Task, UserId = seed.Owner, EndTime = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });
        await factory.WithDb(async db =>
        {
            db.TimeEntries.Add(new TimeEntry { TaskId = seed.Task, UserId = seed.Owner });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    [SqlFact]
    public async Task NotificationRead_ReturnsEnvelope_AndRollsBackWhenOutboxFails()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<INotificationService>().CreateNotificationAsync(seed.Owner, "Test", "Test");
        int id = 0;
        await factory.WithDb(async db => id = (await db.Notifications.SingleAsync()).Id);
        using var client = factory.Client(seed.Owner);
        factory.Failure.EntityName = nameof(OutboxMessage);
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsync($"/api/notification/{id}/read", null)).StatusCode);
        await factory.WithDb(async db => Assert.False((await db.Notifications.SingleAsync()).IsRead));
        factory.Failure.EntityName = null;
        using var response = await client.PutAsync($"/api/notification/{id}/read", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        await factory.WithDb(async db => Assert.True((await db.Notifications.SingleAsync()).IsRead));
    }

    [SqlFact]
    public async Task Cleanup_BadLegacyKeyDoesNotBlockValidFileInSameBatch()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var key = Guid.NewGuid().ToString("N") + ".txt";
        Directory.CreateDirectory(factory.AttachmentRoot);
        await File.WriteAllTextAsync(Path.Combine(factory.AttachmentRoot, key), "test");
        var invalid = new OutboxMessage { Kind = "FileCleanup", Payload = "{\"Key\":\"/uploads/legacy.txt\"}", CreatedAt = DateTime.UtcNow.AddMinutes(-1) };
        var valid = new OutboxMessage { Kind = "FileCleanup", Payload = System.Text.Json.JsonSerializer.Serialize(new { Key = key }) };
        await factory.WithDb(async db => { db.OutboxMessages.AddRange(invalid, valid); await db.SaveChangesAsync(); });
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AttachmentCleanupService>().RunBatchAsync(default);
        Assert.Empty(Directory.GetFiles(factory.AttachmentRoot));
        await factory.WithDb(async db =>
        {
            Assert.Null((await db.OutboxMessages.SingleAsync(m => m.Id == invalid.Id)).CompletedAt);
            Assert.NotNull((await db.OutboxMessages.SingleAsync(m => m.Id == valid.Id)).CompletedAt);
        });
    }

    [SqlFact]
    public async Task Delivery_RollbackSendsNothing_PostCommitFailureKeepsIntentAndBusinessState()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        var sends = 0;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<TaskHub.Application.Repositories.Interfaces.IMutationRunner>();
            var delivery = scope.ServiceProvider.GetRequiredService<DurableDelivery>();
            await Assert.ThrowsAsync<IOException>(() => runner.RunAsync(async () =>
            {
                await delivery.EnqueueAsync("Realtime", new { }, () => { sends++; return Task.CompletedTask; });
                Assert.Equal(0, sends);
                throw new IOException("Injected late failure");
            }));
        }
        Assert.Equal(0, sends);
        await factory.WithDb(async db => Assert.Empty(await db.OutboxMessages.ToListAsync()));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<TaskHub.Application.Repositories.Interfaces.IMutationRunner>();
            var delivery = scope.ServiceProvider.GetRequiredService<DurableDelivery>();
            var db = scope.ServiceProvider.GetRequiredService<TaskHub.Infrastructure.Data.AppDbContext>();
            await runner.RunAsync(async () =>
            {
                (await db.Tasks.SingleAsync(t => t.Id == seed.Task)).Title = "Committed";
                await delivery.EnqueueAsync("Realtime", new { }, async () =>
                {
                    sends++;
                    // A separate connection sees the mutation before delivery runs.
                    await factory.WithDb(async check => Assert.Equal("Committed", (await check.Tasks.SingleAsync(t => t.Id == seed.Task)).Title));
                    throw new IOException("Injected network failure");
                });
            });
        }
        Assert.Equal(1, sends);
        await factory.WithDb(async db =>
        {
            Assert.Equal("Committed", (await db.Tasks.SingleAsync(t => t.Id == seed.Task)).Title);
            Assert.Null((await db.OutboxMessages.SingleAsync()).CompletedAt);
        });
    }

    [SqlFact]
    public async Task TwoOwners_CannotDemoteOrRemoveEachOther_WithoutTransfer()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedTeamAsync();
        await factory.WithDb(async db =>
        {
            (await db.TeamMembers.SingleAsync(m => m.TeamId == seed.Team && m.UserId == seed.Manager)).Role = TeamRole.Owner;
            await db.SaveChangesAsync();
        });
        using var a = factory.Client(seed.Resources.Owner);
        using var b = factory.Client(seed.Manager);
        var results = await Task.WhenAll(
            a.PutAsJsonAsync($"/api/teams/{seed.Team}/members/{seed.Manager}/role", new { role = "Member" }),
            b.DeleteAsync($"/api/teams/{seed.Team}/members/{seed.Resources.Owner}"));
        Assert.All(results, r => Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode));
        await factory.WithDb(async db => Assert.Equal(2, await db.TeamMembers.CountAsync(m => m.TeamId == seed.Team && m.Role == TeamRole.Owner)));
    }

    [SqlFact]
    public async Task Move_OutboxFailure_RollsBackSourceDestinationOrderAndTimestamp()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        var destination = Guid.NewGuid();
        await factory.WithDb(async db =>
        {
            db.Lists.Add(new BoardList { Id = destination, BoardId = seed.Board, Name = "Destination" });
            db.Tasks.Add(new TaskItem { ListId = destination, OwnerId = seed.Owner, Title = "Sibling", Position = 0 });
            await db.SaveChangesAsync();
        });
        factory.Failure.EntityName = nameof(OutboxMessage);
        using var client = factory.Client(seed.Owner);
        var response = await client.PatchAsJsonAsync($"/api/tasks/{seed.Task}/move", new { listId = destination, position = 0 });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDb(async db =>
        {
            var task = await db.Tasks.SingleAsync(t => t.Id == seed.Task);
            Assert.Equal(seed.List, task.ListId);
            Assert.Null(task.UpdatedAt);
            Assert.Equal(0, (await db.Tasks.SingleAsync(t => t.ListId == destination)).Position);
            Assert.Empty(await db.OutboxMessages.ToListAsync());
        });
    }

    [SqlFact]
    public async Task ArchiveRetainsData_DeleteFailureRestoresDescendantsAndCleanupIntent()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        using var form = Upload();
        Assert.True((await client.PostAsync($"/api/tasks/{seed.Task}/attachments", form)).IsSuccessStatusCode);
        Assert.True((await client.PostAsync($"/api/projects/{seed.Project}/archive", null)).IsSuccessStatusCode);
        await factory.WithDb(async db =>
        {
            Assert.NotNull((await db.Projects.SingleAsync()).ArchivedAt);
            Assert.Single(await db.TaskAttachments.ToListAsync());
        });
        Assert.True((await client.PostAsync($"/api/projects/{seed.Project}/restore", null)).IsSuccessStatusCode);
        factory.Failure.EntityName = nameof(Project);
        factory.Failure.State = EntityState.Deleted;
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.DeleteAsync($"/api/projects/{seed.Project}")).StatusCode);
        await factory.WithDb(async db =>
        {
            Assert.Null((await db.Projects.SingleAsync()).ArchivedAt);
            Assert.Single(await db.Boards.ToListAsync());
            Assert.Single(await db.Lists.ToListAsync());
            Assert.Equal(2, await db.Tasks.CountAsync());
            Assert.Single(await db.TaskAttachments.ToListAsync());
            Assert.Empty(await db.OutboxMessages.Where(m => m.Kind == "FileCleanup").ToListAsync());
        });
        Assert.Single(Directory.GetFiles(factory.AttachmentRoot));
    }

    [SqlFact]
    public async Task StorageWriteFailure_CreatesNoMetadataOrActivity()
    {
        await using var factory = new Phase2SqlFactory();
        factory.ConfigureServices = services => services.AddSingleton<IAttachmentStorage>(new FailingStorage { FailWrite = true });
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        using var form = Upload();
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsync($"/api/tasks/{seed.Task}/attachments", form)).StatusCode);
        await factory.WithDb(async db =>
        {
            Assert.Empty(await db.TaskAttachments.ToListAsync());
            Assert.Empty(await db.TaskActivityLogs.ToListAsync());
            Assert.Empty(await db.OutboxMessages.ToListAsync());
        });
    }

    [SqlFact]
    public async Task FileDeleteFailure_CommitsMetadataRemoval_AndCleanupRetryRemovesFile()
    {
        await using var factory = new Phase2SqlFactory();
        var storage = new FailingStorage
        {
            Inner = new LocalAttachmentStorage(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Attachments:PrivateRoot"] = factory.AttachmentRoot }).Build())
        };
        factory.ConfigureServices = services => services.AddSingleton<IAttachmentStorage>(storage);
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        using var form = Upload();
        Assert.True((await client.PostAsync($"/api/tasks/{seed.Task}/attachments", form)).IsSuccessStatusCode);
        Guid attachmentId = default;
        await factory.WithDb(async db => attachmentId = (await db.TaskAttachments.SingleAsync()).Id);
        storage.FailDelete = true;
        Assert.True((await client.DeleteAsync($"/api/tasks/{seed.Task}/attachments/{attachmentId}")).IsSuccessStatusCode);
        await factory.WithDb(async db =>
        {
            Assert.Empty(await db.TaskAttachments.ToListAsync());
            Assert.Null((await db.OutboxMessages.SingleAsync(m => m.Kind == "FileCleanup")).CompletedAt);
        });
        Assert.Single(Directory.GetFiles(factory.AttachmentRoot));
        storage.FailDelete = false;
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AttachmentCleanupService>().RunBatchAsync(default);
        Assert.Empty(Directory.GetFiles(factory.AttachmentRoot));
        await factory.WithDb(async db => Assert.NotNull((await db.OutboxMessages.SingleAsync(m => m.Kind == "FileCleanup")).CompletedAt));
    }

    [SqlFact]
    public async Task WorkspaceSlugConstraint_AllowsOtherWorkspace_RejectsSameWorkspace()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedTeamAsync();
        await factory.WithDb(async db =>
        {
            var team = new Team { Name = "Other workspace", CreatedById = seed.Resources.Owner };
            db.Teams.Add(team);
            db.Projects.Add(new Project { Name = "Allowed", Slug = "private", OwnerId = seed.Resources.Owner, WorkspaceId = team.Id, ProjectType = ProjectType.Team });
            await db.SaveChangesAsync();
        });
        await factory.WithDb(async db =>
        {
            db.Projects.Add(new Project { Name = "Rejected", Slug = "private", OwnerId = seed.Manager, WorkspaceId = seed.Team, ProjectType = ProjectType.Team });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    private static MultipartFormDataContent Upload()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("private test content"), "file", "test.txt");
        return form;
    }

    private sealed class FailingStorage : IAttachmentStorage
    {
        public IAttachmentStorage Inner { get; init; } = null!;
        public bool FailWrite { get; set; }
        public bool FailDelete { get; set; }
        public Task<StoredAttachment> StoreAsync(Stream content, string name, CancellationToken ct) =>
            FailWrite ? Task.FromException<StoredAttachment>(new IOException("Injected write failure")) : Inner.StoreAsync(content, name, ct);
        public Task<Stream> OpenAsync(string key, CancellationToken ct) => Inner.OpenAsync(key, ct);
        public Task DeleteAsync(string key, CancellationToken ct) =>
            FailDelete ? Task.FromException(new IOException("Injected delete failure")) : Inner.DeleteAsync(key, ct);
    }
}
