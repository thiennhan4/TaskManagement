using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public sealed class Phase2SqlMutationTests
{
    private static async Task<HttpResponseMessage[]> Race(Func<Task<HttpResponseMessage>> first, Func<Task<HttpResponseMessage>> second)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a = Task.Run(async () => { await gate.Task; return await first(); });
        var b = Task.Run(async () => { await gate.Task; return await second(); });
        gate.SetResult();
        return await Task.WhenAll(a, b);
    }

    [SqlFact]
    public async Task Refresh_ConcurrentRequests_ExactlyOneSuccessAndOneSuccessor()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        var original = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await factory.WithDb(async db => { db.RefreshTokens.Add(new RefreshToken { UserId = seed.Owner, Token = original, ExpiresAt = DateTime.UtcNow.AddDays(1) }); await db.SaveChangesAsync(); });
        using var a = factory.Client(); using var b = factory.Client();
        a.DefaultRequestHeaders.Add("Cookie", "refreshToken=" + original);
        b.DefaultRequestHeaders.Add("Cookie", "refreshToken=" + original);
        var results = await Race(() => a.PostAsync("/api/auth/refresh", null), () => b.PostAsync("/api/auth/refresh", null));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        var loser = Assert.Single(results, r => r.StatusCode == HttpStatusCode.Unauthorized);
        Assert.False(loser.Headers.Contains("Set-Cookie"));
        await factory.WithDb(async db => { Assert.Equal(2, await db.RefreshTokens.CountAsync()); Assert.Equal(1, await db.RefreshTokens.CountAsync(t => !t.IsRevoked)); });
    }

    [SqlFact]
    public async Task Refresh_InsertFailure_RollsBackConsumption()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        var original = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await factory.WithDb(async db => { db.RefreshTokens.Add(new RefreshToken { UserId = seed.Owner, Token = original, ExpiresAt = DateTime.UtcNow.AddDays(1) }); await db.SaveChangesAsync(); });
        factory.Failure.EntityName = nameof(RefreshToken);
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Cookie", "refreshToken=" + original);
        var response = await client.PostAsync("/api/auth/refresh", null); Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDb(async db => Assert.False((await db.RefreshTokens.SingleAsync()).IsRevoked));
    }

    [SqlFact]
    public async Task Timer_ConcurrentStarts_OneActiveRow()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        using var a = factory.Client(seed.Owner); using var b = factory.Client(seed.Owner);
        var results = await Race(() => a.PostAsJsonAsync("/api/timetracking/start", new { taskId = seed.Task }), () => b.PostAsJsonAsync("/api/timetracking/start", new { taskId = seed.Task }));
        Assert.True(results.Count(r => r.IsSuccessStatusCode) == 1, string.Join(",", results.Select(r => r.StatusCode))); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        await factory.WithDb(async db => Assert.Equal(1, await db.TimeEntries.CountAsync(t => t.UserId == seed.Owner && t.EndTime == null)));
    }

    [SqlFact]
    public async Task ProjectCreation_ActivityFailure_RollsBackProjectBoardMembershipNotification()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedTeamAsync();
        factory.Failure.EntityName = nameof(ProjectActivityLog);
        using var client = factory.Client(seed.Resources.Owner);
        var response = await client.PostAsJsonAsync("/api/projects", new { name = "Rollback", slug = "rollback", projectType = "Team", workspaceId = seed.Team });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDb(async db => { Assert.Equal(1, await db.Projects.CountAsync()); Assert.Equal(1, await db.Boards.CountAsync()); Assert.Equal(4, await db.ProjectMembers.CountAsync()); Assert.Empty(await db.Notifications.ToListAsync()); Assert.Empty(await db.OutboxMessages.ToListAsync()); });
    }

    [SqlFact]
    public async Task Comment_ActivityFailure_RollsBackCommentAndDelivery()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        factory.Failure.EntityName = nameof(TaskActivityLog); using var client = factory.Client(seed.Owner);
        var response = await client.PostAsJsonAsync($"/api/tasks/{seed.Task}/comments", new { content = "Must roll back" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDb(async db => { Assert.Empty(await db.Comments.ToListAsync()); Assert.Empty(await db.OutboxMessages.ToListAsync()); });
    }

    [SqlFact]
    public async Task Status_ActivityFailure_RollsBackTask()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        factory.Failure.EntityName = nameof(TaskActivityLog); using var client = factory.Client(seed.Owner);
        var response = await client.PatchAsJsonAsync($"/api/tasks/{seed.Task}/status", new { newStatus = "Done" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDb(async db => Assert.Equal(TaskItemStatus.Todo, (await db.Tasks.SingleAsync(t => t.Id == seed.Task)).Status));
    }

    [SqlFact]
    public async Task Invitation_ActivityFailure_RollsBackAcceptedFlagAndMembership()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedTeamAsync();
        var token = Guid.NewGuid().ToString("N");
        await factory.WithDb(async db => { db.ProjectInvitations.Add(new ProjectInvitation { ProjectId = seed.Resources.Project, InvitedByUserId = seed.Resources.Owner, InviteeEmail = "outsider@example.invalid", Role = ProjectRole.Member, Token = token, ExpiresAt = DateTime.UtcNow.AddDays(1) }); await db.SaveChangesAsync(); });
        factory.Failure.EntityName = nameof(ProjectActivityLog); using var client = factory.Client(seed.Resources.Outsider);
        var response = await client.PostAsJsonAsync($"/api/projects/{seed.Resources.Project}/members/accept-invite", new { token });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDb(async db => { Assert.False((await db.ProjectInvitations.SingleAsync()).IsAccepted); Assert.False(await db.ProjectMembers.AnyAsync(m => m.UserId == seed.Resources.Outsider)); });
    }

    [SqlFact]
    public async Task ProjectDelete_CascadesDescendants_QueuesPrivateFileCleanup()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        await factory.WithDb(async db => { db.TaskAttachments.Add(new TaskAttachment { TaskId = seed.Task, UploadedByUserId = seed.Owner, FileName = "test.txt", FilePath = Guid.NewGuid().ToString("N") + ".txt", ContentType = "text/plain" }); db.TimeEntries.Add(new TimeEntry { TaskId = seed.Task, UserId = seed.Owner }); await db.SaveChangesAsync(); });
        using var client = factory.Client(seed.Owner); var response = await client.DeleteAsync($"/api/projects/{seed.Project}"); Assert.True(response.IsSuccessStatusCode);
        await factory.WithDb(async db => { Assert.Empty(await db.Projects.ToListAsync()); Assert.Empty(await db.Boards.ToListAsync()); Assert.Empty(await db.Tasks.ToListAsync()); Assert.Empty(await db.TimeEntries.ToListAsync()); Assert.Empty(await db.TaskAttachments.ToListAsync()); Assert.Single(await db.OutboxMessages.Where(m => m.Kind == "FileCleanup").ToListAsync()); });
    }

    [SqlFact]
    public async Task SlugConstraint_AllowsDifferentPersonalOwners_RejectsSameScope()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedAsync();
        await factory.WithDb(async db => { db.Projects.Add(new Project { OwnerId = seed.Outsider, Name = "Other", Slug = "private", ProjectType = ProjectType.Personal }); await db.SaveChangesAsync(); });
        await factory.WithDb(async db => { db.Projects.Add(new Project { OwnerId = seed.Owner, Name = "Duplicate", Slug = "private", ProjectType = ProjectType.Personal }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); });
    }

    [SqlFact]
    public async Task Ownership_ConcurrentTransfers_ExactlyOneNewProjectOwner()
    {
        await using var factory = new Phase2SqlFactory(); await factory.InitializeAsync(); var seed = await factory.SeedTeamAsync();
        using var a = factory.Client(seed.Resources.Owner); using var b = factory.Client(seed.Resources.Owner);
        var results = await Race(() => a.PostAsJsonAsync($"/api/v1/projects/{seed.Resources.Project}/transfer-ownership", new { targetUserId = seed.Member }), () => b.PostAsJsonAsync($"/api/v1/projects/{seed.Resources.Project}/transfer-ownership", new { targetUserId = seed.Manager }));
        Assert.Single(results, r => r.IsSuccessStatusCode); Assert.Single(results, r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden);
        await factory.WithDb(async db => { var owner = await db.ProjectMembers.SingleAsync(m => m.ProjectId == seed.Resources.Project && m.Role == ProjectRole.Owner); Assert.Equal(owner.UserId, (await db.Projects.SingleAsync()).OwnerId); });
    }
}
