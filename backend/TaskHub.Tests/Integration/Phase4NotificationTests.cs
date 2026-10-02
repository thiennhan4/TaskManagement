using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class Phase4NotificationTests
{
    [Fact]
    public async Task NotificationPage_HasScopedTotalsStablePagesAndIndependentUnreadCount()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var type = new NotificationType { Code = "PHASE4", Name = "Phase 4" };
            db.NotificationTypes.Add(type);
            for (var i = 0; i < 25; i++)
                db.Notifications.Add(new Notification { UserId = seed.Owner, NotificationType = type, Title = "Visible", Message = "Test", CreatedAt = new DateTime(2026, 1, 1), IsRead = i < 3 });
            db.Notifications.Add(new Notification { UserId = seed.Owner, NotificationType = type, Title = "Expired", Message = "Test", ExpiresAt = DateTime.UtcNow.AddDays(-1) });
            db.Notifications.Add(new Notification { UserId = seed.Outsider, NotificationType = type, Title = "Other user", Message = "Test" });
            await db.SaveChangesAsync();
        }
        using var client = factory.Client(seed.Owner);
        var ids = new HashSet<int>();
        for (var page = 1; page <= 3; page++)
        {
            using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/notifications?page={page}&pageSize=10"));
            var data = json.RootElement.GetProperty("data");
            Assert.Equal(25, data.GetProperty("totalItems").GetInt32());
            Assert.Equal(3, data.GetProperty("totalPages").GetInt32());
            foreach (var row in data.GetProperty("items").EnumerateArray())
            {
                Assert.Equal("Visible", row.GetProperty("title").GetString());
                Assert.True(ids.Add(row.GetProperty("id").GetInt32()));
            }
        }
        Assert.Equal(25, ids.Count);
        using var count = JsonDocument.Parse(await client.GetStringAsync("/api/notification/unread-count"));
        Assert.Equal(22, count.RootElement.GetProperty("data").GetInt32());
        using var legacy = JsonDocument.Parse(await client.GetStringAsync("/api/notification?pageSize=10"));
        Assert.Equal(JsonValueKind.Array, legacy.RootElement.GetProperty("data").ValueKind);
        using var outsider = factory.Client(seed.Outsider);
        using var outside = JsonDocument.Parse(await outsider.GetStringAsync("/api/v1/notifications"));
        Assert.Equal(1, outside.RootElement.GetProperty("data").GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task NotificationPage_RejectsInvalidBounds(string query)
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/notifications?" + query)).StatusCode);
    }
}
