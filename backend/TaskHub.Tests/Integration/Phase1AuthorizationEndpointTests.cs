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

public class Phase1AuthorizationEndpointTests
{
    [Fact]
    public async Task Assignment_OmissionPreservesAssignee_ChangeRequiresAssignPermission()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        using var member = factory.Client(seed.Member);
        var path = $"/api/tasks/{seed.Resources.Task}";
        using var edited = await member.PutAsJsonAsync(path, new { title = "Renamed" });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        using var json = JsonDocument.Parse(await edited.Content.ReadAsStringAsync());
        Assert.Equal(seed.Member, json.RootElement.GetProperty("data").GetProperty("assignedToId").GetGuid());
        using var unassign = await member.PutAsJsonAsync(path, new { title = "Denied", assignedToId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Forbidden, unassign.StatusCode);
        using var dedicated = await member.PatchAsJsonAsync(path + "/assign", new { assignedToUserId = seed.Manager });
        Assert.Equal(HttpStatusCode.Forbidden, dedicated.StatusCode);
        using var manager = factory.Client(seed.Manager);
        using var changed = await manager.PutAsJsonAsync(path, new { title = "Allowed", assignedToId = seed.Manager });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        using var ineligible = await manager.PutAsJsonAsync(path, new { title = "Denied", assignedToId = seed.Resources.Outsider });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ineligible.StatusCode);
    }

    [Theory]
    [InlineData("valid", HttpStatusCode.OK)]
    [InlineData("wrong-recipient", HttpStatusCode.Forbidden)]
    [InlineData("removed-recipient", HttpStatusCode.Forbidden)]
    [InlineData("removed-inviter", HttpStatusCode.Forbidden)]
    [InlineData("inactive-recipient", HttpStatusCode.Forbidden)]
    [InlineData("expired", HttpStatusCode.UnprocessableEntity)]
    [InlineData("invalid", HttpStatusCode.UnprocessableEntity)]
    public async Task TaskInvitation_RechecksRecipientAndInviter_WithoutCreatingMembership(string scenario, HttpStatusCode expected)
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        var token = Guid.NewGuid().ToString("N");
        int membershipCount;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.TaskInvitations.Add(new TaskInvitation { TaskId = seed.Resources.Task, InvitedByUserId = seed.Manager,
                InviteeEmail = "MEMBER@example.invalid", Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(scenario == "expired" ? -1 : 1) });
            var removed = scenario == "removed-recipient" ? seed.Member : scenario == "removed-inviter" ? seed.Manager : Guid.Empty;
            db.TeamMembers.RemoveRange(await db.TeamMembers.Where(m => m.UserId == removed).ToListAsync());
            db.ProjectMembers.RemoveRange(await db.ProjectMembers.Where(m => m.UserId == removed).ToListAsync());
            if (scenario == "inactive-recipient") (await db.Users.SingleAsync(u => u.Id == seed.Member)).IsActive = false;
            await db.SaveChangesAsync();
            membershipCount = await db.TeamMembers.CountAsync();
        }
        using var client = factory.Client(scenario == "wrong-recipient" ? seed.Resources.Outsider : seed.Member);
        using var response = await client.PostAsJsonAsync("/api/tasks/accept-invite", new { token = scenario == "invalid" ? "missing" : token });
        Assert.Equal(expected, response.StatusCode);
        using var check = factory.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(membershipCount, await context.TeamMembers.CountAsync());
        Assert.Equal(scenario == "valid", (await context.TaskInvitations.SingleAsync()).IsAccepted);
    }

    [Fact]
    public async Task TaskInvitation_UnauthorizedInviterCannotAssign()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        using var member = factory.Client(seed.Member);
        using var denied = await member.PostAsJsonAsync($"/api/tasks/{seed.Resources.Task}/invite", new { email = "manager@example.invalid" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var manager = factory.Client(seed.Manager);
        using var allowed = await manager.PostAsJsonAsync($"/api/tasks/{seed.Resources.Task}/invite", new { email = "member@example.invalid" });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task LegacyTeamAssignment_RejectsNonmembersAndInactiveRecipients()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Boards.SingleAsync()).ProjectId = null;
            (await db.Users.SingleAsync(u => u.Id == seed.Member)).IsActive = false;
            await db.SaveChangesAsync();
        }
        using var manager = factory.Client(seed.Manager);
        foreach (var id in new[] { seed.Resources.Outsider, seed.Member })
        {
            using var denied = await manager.PatchAsJsonAsync($"/api/tasks/{seed.Resources.Task}/assign", new { assignedToUserId=id });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, denied.StatusCode);
        }
        using var allowed = await manager.PatchAsJsonAsync($"/api/tasks/{seed.Resources.Task}/assign", new { assignedToUserId=seed.Manager });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task LegacyOwnerInvitationAndUndefinedRoles_CannotChangeOwnership()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        var token = Guid.NewGuid().ToString("N");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ProjectInvitations.Add(new ProjectInvitation { ProjectId=seed.Resources.Project, InvitedByUserId=seed.Resources.Owner,
                InviteeEmail="outsider@example.invalid", Role=ProjectRole.Owner, Token=token });
            await db.SaveChangesAsync();
        }
        using var outsider = factory.Client(seed.Resources.Outsider);
        using var denied = await outsider.PostAsJsonAsync("/api/projects/accept-invite", new { token });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var owner = factory.Client(seed.Resources.Owner);
        using var role = await owner.PatchAsJsonAsync($"/api/projects/{seed.Resources.Project}/members/{seed.Member}/role", new { role=999 });
        Assert.Equal(HttpStatusCode.BadRequest, role.StatusCode);
        using var removeOwner = await owner.DeleteAsync($"/api/projects/{seed.Resources.Project}/members/{seed.Resources.Owner}");
        Assert.Equal(HttpStatusCode.Forbidden, removeOwner.StatusCode);
        using var check = factory.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False((await context.ProjectInvitations.SingleAsync()).IsAccepted);
        Assert.Equal(seed.Resources.Owner, (await context.Projects.SingleAsync()).OwnerId);
    }

    [Fact]
    public async Task Ownership_OrdinaryPromotionDenied_ExplicitTransferKeepsOneOwner()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        var project = seed.Resources.Project;
        using var manager = factory.Client(seed.Manager);
        using var projectPromotion = await manager.PatchAsJsonAsync($"/api/projects/{project}/members/{seed.Member}/role", new { role = "Owner" });
        Assert.Equal(HttpStatusCode.Forbidden, projectPromotion.StatusCode);
        using var teamPromotion = await manager.PutAsJsonAsync($"/api/teams/{seed.Team}/members/{seed.Member}/role", new { role = "Owner" });
        Assert.Equal(HttpStatusCode.Forbidden, teamPromotion.StatusCode);
        using var projectInvite = await manager.PostAsJsonAsync($"/api/projects/{project}/members/invite", new { email = "outsider@example.invalid", role = "Owner" });
        Assert.Equal(HttpStatusCode.Forbidden, projectInvite.StatusCode);
        using var teamAdd = await manager.PostAsJsonAsync($"/api/teams/{seed.Team}/members", new { email = "outsider@example.invalid", role = "Owner" });
        Assert.Equal(HttpStatusCode.Forbidden, teamAdd.StatusCode);
        using var transferDenied = await manager.PostAsJsonAsync($"/api/v1/projects/{project}/transfer-ownership", new { targetUserId = seed.Manager });
        Assert.Equal(HttpStatusCode.Forbidden, transferDenied.StatusCode);
        using var owner = factory.Client(seed.Resources.Owner);
        using var transferred = await owner.PostAsJsonAsync($"/api/v1/projects/{project}/transfer-ownership", new { targetUserId = seed.Member });
        Assert.Equal(HttpStatusCode.OK, transferred.StatusCode);
        using var teamTransferred = await owner.PostAsJsonAsync($"/api/v1/teams/{seed.Team}/transfer-ownership", new { targetUserId = seed.Member });
        Assert.Equal(HttpStatusCode.OK, teamTransferred.StatusCode);
        using var check = factory.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(seed.Member, (await db.Projects.SingleAsync()).OwnerId);
        Assert.Equal(seed.Member, (await db.ProjectMembers.SingleAsync(m => m.Role == ProjectRole.Owner)).UserId);
        Assert.Equal(seed.Member, (await db.TeamMembers.SingleAsync(m => m.Role == TeamRole.Owner)).UserId);
    }

    [Fact]
    public async Task Lists_DistinguishDeniedMissingEmpty_AndAllowProjectManager()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        using var manager = factory.Client(seed.Manager);
        using var member = factory.Client(seed.Member);
        using var outsider = factory.Client(seed.Resources.Outsider);
        var path = $"/api/boardlists/board/{seed.Resources.Board}";
        using var read = await member.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var denied = await outsider.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var missing = await manager.GetAsync($"/api/boardlists/board/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var memberWrite = await member.PostAsJsonAsync("/api/boardlists", new { boardId = seed.Resources.Board, name = "Denied" });
        Assert.Equal(HttpStatusCode.Forbidden, memberWrite.StatusCode);
        using var created = await manager.PostAsJsonAsync("/api/boardlists", new { boardId = seed.Resources.Board, name = "Allowed" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        using var updated = await manager.PutAsJsonAsync($"/api/boardlists/{id}", new { name = "Updated", position = 1 });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var deleted = await manager.DeleteAsync($"/api/boardlists/{id}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
    }

    [Fact]
    public async Task TimeManualHistoryAndOwnerMutations_EnforcePermissions()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        var payload = new { taskId = seed.Resources.Task, startTime = DateTime.UtcNow.AddHours(-2), endTime = DateTime.UtcNow.AddHours(-1) };
        using var outsider = factory.Client(seed.Resources.Outsider);
        using var denied = await outsider.PostAsJsonAsync("/api/timetracking/manual", payload);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var member = factory.Client(seed.Member);
        using var created = await member.PostAsJsonAsync("/api/timetracking/manual", payload);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        using var owner = factory.Client(seed.Resources.Owner);
        using var deleteDenied = await owner.DeleteAsync($"/api/timetracking/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteDenied.StatusCode);
        using var history = await member.GetAsync($"/api/v1/timetracking/task/{seed.Resources.Task}?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var page = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        Assert.Equal(1, page.RootElement.GetProperty("data").GetProperty("totalItems").GetInt32());
        using var deleted = await member.DeleteAsync($"/api/timetracking/{id}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
    }
}
