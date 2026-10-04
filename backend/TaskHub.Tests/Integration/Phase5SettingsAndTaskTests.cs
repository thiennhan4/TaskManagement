using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Infrastructure.Data;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class Phase5SettingsAndTaskTests
{
    [Fact]
    public async Task Profile_UpdatePersistsAndMeReturnsSupportedFields()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        using var response = await client.PutAsJsonAsync("/api/settings/profile", new { fullName = "Saved Profile", avatarUrl = "https://example.invalid/avatar.png" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var me = await client.GetAsync("/api/auth/me");
        using var json = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        var user = json.RootElement.GetProperty("data");
        Assert.Equal("Saved Profile", user.GetProperty("fullName").GetString());
        Assert.Equal("https://example.invalid/avatar.png", user.GetProperty("avatarUrl").GetString());
        Assert.False(user.TryGetProperty("bio", out _));
        using var scope = factory.Services.CreateScope();
        Assert.Equal("Saved Profile", (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Id == seed.Owner)).FullName);
    }

    [Fact]
    public async Task Password_RejectsWrongCurrentPasswordAndRevokesRefreshTokensOnSuccess()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Users.SingleAsync(u => u.Id == seed.Owner)).PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-pass");
            db.RefreshTokens.Add(new TaskHub.Domain.Entities.RefreshToken { UserId = seed.Owner, Token = "synthetic-phase5-token", ExpiresAt = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        }
        using var client = factory.Client(seed.Owner);
        using var failed = await client.PutAsJsonAsync("/api/settings/password", new { currentPassword = "wrong", newPassword = "new-pass" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, failed.StatusCode);
        using var invalid = await client.PutAsJsonAsync("/api/settings/password", new { currentPassword = "old-pass", newPassword = "123" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var success = await client.PutAsJsonAsync("/api/settings/password", new { currentPassword = "old-pass", newPassword = "new-pass" });
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        using var check = factory.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(BCrypt.Net.BCrypt.Verify("new-pass", (await context.Users.SingleAsync(u => u.Id == seed.Owner)).PasswordHash));
        Assert.True((await context.RefreshTokens.SingleAsync()).IsRevoked);
        using var stillActive = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, stillActive.StatusCode);
    }

    [Fact]
    public async Task Task_CapabilitiesAndEligibleAssigneesRespectAuthorizationAndPaging()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        using var manager = factory.Client(seed.Manager);
        using var member = factory.Client(seed.Member);
        var path = $"/api/v1/tasks/{seed.Resources.Task}";
        using var detail = await manager.GetAsync(path);
        using var json = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("capabilities").GetProperty("canAssign").GetBoolean());
        using var denied = await member.GetAsync(path + "/eligible-assignees");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var page = await manager.GetAsync(path + "/eligible-assignees?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        using var result = JsonDocument.Parse(await page.Content.ReadAsStringAsync());
        Assert.Equal(1, result.RootElement.GetProperty("data").GetProperty("items").GetArrayLength());
        using var invalid = await manager.GetAsync(path + "/eligible-assignees?pageSize=1000");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var outsider = factory.Client(seed.Resources.Outsider);
        using var forbidden = await outsider.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var missing = await manager.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
