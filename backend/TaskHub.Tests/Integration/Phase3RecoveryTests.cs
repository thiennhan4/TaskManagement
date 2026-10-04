using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class Phase3RecoveryTests
{
    [SqlFact]
    public async Task CollectionsOnSql_ArePagedAuthorizedAndHaveExplicitCounts()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var team = await factory.SeedTeamAsync();
        var seed = team.Resources;
        var now = DateTime.UtcNow;
        await factory.WithDb(async db => {
            for (var i = 0; i < 23; i++)
            {
                var user = new AppUser { Email=$"page{i}@example.invalid", FullName=$"Page {i}", PasswordHash="SYNTHETIC_HASH" };
                db.Users.Add(user);
                db.TeamMembers.Add(new TeamMember { TeamId=team.Team, UserId=user.Id, Role=TeamRole.Member, JoinedAt=now });
                db.ProjectMembers.Add(new ProjectMember { ProjectId=seed.Project, UserId=user.Id, Role=ProjectRole.Member, JoinedAt=now });
                db.Comments.Add(new Comment { TaskId=seed.Task, UserId=seed.Owner, Content=$"Comment {i}", CreatedAt=now });
                db.TaskAttachments.Add(new TaskAttachment { TaskId=seed.Task, UploadedByUserId=seed.Owner, FileName=$"file{i}.txt", FilePath="private/synthetic", ContentType="text/plain", UploadedAt=now });
                db.TaskActivityLogs.Add(new TaskActivityLog { TaskId=seed.Task, UserId=seed.Owner, Action=ActivityLogAction.Created, CreatedAt=now });
                db.TimeEntries.Add(new TimeEntry { TaskId=seed.Task, UserId=seed.Owner, StartTime=now.AddHours(-1), EndTime=now, DurationSeconds=60 });
                db.Projects.Add(new Project { Name=$"Page {i}", Slug=$"page-{i}", OwnerId=seed.Owner, WorkspaceId=team.Team, ProjectType=ProjectType.Team });
            }
            db.Comments.Add(new Comment { TaskId=seed.Task, UserId=seed.Owner, Content="Deleted", IsDeleted=true });
            await db.SaveChangesAsync();
        });
        using var owner = factory.Client(seed.Owner);
        using var outsider = factory.Client(seed.Outsider);
        foreach (var (url, count, denied) in new[] {
            ($"/api/v1/projects?workspaceId={team.Team}",24,true),
            ($"/api/v1/teams/{team.Team}/members",26,true),
            ($"/api/v1/projects/{seed.Project}/members",27,true),
            ($"/api/v1/tasks/{seed.Task}/comments",23,true),
            ($"/api/v1/tasks/{seed.Task}/attachments",23,true),
            ($"/api/v1/tasks/{seed.Task}/activity-logs",23,true),
            ($"/api/v1/timetracking/task/{seed.Task}",23,true),
            ("/api/v1/timetracking/my-entries",23,false)
        })
        {
            var seen = new HashSet<Guid>();
            for (var page = 1; page <= (int)Math.Ceiling(count/10.0); page++)
            {
                var separator = url.Contains('?') ? '&' : '?';
                using var json = JsonDocument.Parse(await owner.GetStringAsync($"{url}{separator}page={page}&pageSize=10"));
                var data = json.RootElement.GetProperty("data");
                Assert.Equal(count,data.GetProperty("totalItems").GetInt32());
                Assert.InRange(data.GetProperty("items").GetArrayLength(),1,10);
                foreach (var item in data.GetProperty("items").EnumerateArray()) Assert.True(seen.Add(item.GetProperty("id").GetGuid()));
                Assert.DoesNotContain("private/synthetic",json.RootElement.GetRawText());
                Assert.DoesNotContain("passwordHash",json.RootElement.GetRawText());
            }
            Assert.Equal(count,seen.Count);
            if (denied) Assert.Equal(HttpStatusCode.Forbidden,(await outsider.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await owner.GetAsync(url+(url.Contains('?')?'&':'?')+"pageSize=101")).StatusCode);
        }
        using var header = JsonDocument.Parse(await owner.GetStringAsync($"/api/v1/tasks/{seed.Task}"));
        var detail = header.RootElement.GetProperty("data");
        Assert.Equal(23,detail.GetProperty("commentsCount").GetInt32());
        Assert.Equal(23,detail.GetProperty("attachmentsCount").GetInt32());
        Assert.Empty(detail.GetProperty("comments").EnumerateArray());
        Assert.Empty(detail.GetProperty("attachments").EnumerateArray());
        Assert.Empty(detail.GetProperty("activityLogs").EnumerateArray());
        using var project = JsonDocument.Parse(await owner.GetStringAsync($"/api/v1/projects/{seed.Project}"));
        Assert.Equal(27,project.RootElement.GetProperty("data").GetProperty("memberCount").GetInt32());
        Assert.Equal(1,project.RootElement.GetProperty("data").GetProperty("boardCount").GetInt32());
        using var teamHeader = JsonDocument.Parse(await owner.GetStringAsync($"/api/v1/teams/{team.Team}"));
        Assert.Equal(26,teamHeader.RootElement.GetProperty("data").GetProperty("memberCount").GetInt32());
        Assert.False(teamHeader.RootElement.GetProperty("data").TryGetProperty("members",out _));
        foreach (var suffix in new[] { "report", "my-entries" })
            Assert.Equal(HttpStatusCode.BadRequest,(await owner.GetAsync($"/api/v1/timetracking/{suffix}?from=2025-01-01&to=2026-01-03")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.GetAsync("/api/v1/timetracking/report?from=2026-02-01&to=2026-01-01")).StatusCode);
    }

    [SqlFact]
    public async Task BulkReadAndRevoke_AreSingleScopedUpdatesWithoutTracking()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        await factory.WithDb(async db => {
            var type = new NotificationType { Code="Phase3Test", Name="Phase 3 test" };
            db.NotificationTypes.Add(type);
            foreach (var user in new[] { seed.Owner, seed.Outsider })
                for (var i=0;i<25;i++)
                {
                    db.Notifications.Add(new Notification { UserId=user, NotificationType=type, Title="Test", Message="Test" });
                    db.RefreshTokens.Add(new RefreshToken { UserId=user, Token=Guid.NewGuid().ToString(), ExpiresAt=DateTime.UtcNow.AddDays(1) });
                }
            await db.SaveChangesAsync();
        });
        var counter = new QueryCounter();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(factory.ConnectionString).AddInterceptors(counter).Options);
        Assert.True(await new NotificationRepository(context).MarkReadAsync(seed.Owner,null,default));
        Assert.Single(counter.Commands);
        Assert.StartsWith("UPDATE",counter.Commands.Single());
        counter.Commands.Clear();
        await new RefreshTokenRepository(context).RevokeAllAsync(seed.Owner);
        Assert.Single(counter.Commands);
        Assert.StartsWith("UPDATE",counter.Commands.Single());
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(25,await context.Notifications.CountAsync(n=>n.UserId==seed.Owner && n.IsRead));
        Assert.Equal(25,await context.Notifications.CountAsync(n=>n.UserId==seed.Outsider && !n.IsRead));
        Assert.Equal(25,await context.RefreshTokens.CountAsync(t=>t.UserId==seed.Owner && t.IsRevoked));
        Assert.Equal(25,await context.RefreshTokens.CountAsync(t=>t.UserId==seed.Outsider && !t.IsRevoked));
    }
}
