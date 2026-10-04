using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using TaskHub.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace TaskHub.Tests.Integration;

public class Phase3SqlReadTests(ITestOutputHelper output)
{
    [SqlFact]
    public async Task EmailEquality_UsesColumnCollationAndPreservesCaseInsensitiveInvitationMatching()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        await factory.WithDb(async db => {
            db.ProjectInvitations.Add(new ProjectInvitation { ProjectId=seed.Project, InvitedByUserId=seed.Owner, InviteeEmail="Mixed@Example.Invalid" });
            await db.SaveChangesAsync();
        });
        var counter = new QueryCounter();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(factory.ConnectionString).AddInterceptors(counter).Options);
        foreach (var caseSensitive in new[] { false, true })
        {
            // Only this generated disposable database is altered. No application schema or migration is touched.
            await context.Database.ExecuteSqlRawAsync(caseSensitive
                ? "ALTER TABLE ProjectInvitations ALTER COLUMN InviteeEmail nvarchar(max) COLLATE Latin1_General_100_CS_AS NOT NULL"
                : "ALTER TABLE ProjectInvitations ALTER COLUMN InviteeEmail nvarchar(max) COLLATE Latin1_General_100_CI_AS NOT NULL");
            counter.Commands.Clear();
            var invitation = await new ProjectRepository(context).GetActiveInvitationAsync(seed.Project," MIXED@EXAMPLE.INVALID ");
            Assert.NotNull(invitation);
            Assert.Equal(caseSensitive,counter.Commands.Last().Contains("LOWER("));
            Assert.Null(await new ProjectRepository(context).GetActiveInvitationAsync(Guid.NewGuid(),"mixed@example.invalid"));
            context.ChangeTracker.Clear();
        }
        counter.Commands.Clear();
        Assert.Equal(seed.Owner,(await new UserRepository(context).GetByEmailAsync(" OWNER@EXAMPLE.INVALID "))!.Id);
        Assert.DoesNotContain("LOWER(",counter.Commands.Last());
    }

    [SqlFact]
    public async Task Analytics_PreservesRollingCutoffsLatestCompletionAndThirtyDayBurndown()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed = await factory.SeedAsync();
        var from = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(45);
        await factory.WithDb(async db => {
            var original = await db.Tasks.SingleAsync(t => t.Id == seed.Task);
            original.CreatedAt = from.AddDays(-2);
            original.Status = TaskItemStatus.Done;
            var added = new TaskItem { ListId=seed.List, OwnerId=seed.Owner, Title="After cutoff", CreatedAt=from.AddHours(1) };
            var invalidDuration = new TaskItem { ListId=seed.List, OwnerId=seed.Owner, Title="Imported chronology", CreatedAt=from.AddDays(20), Status=TaskItemStatus.Done };
            db.Tasks.AddRange(added, invalidDuration);
            db.TaskActivityLogs.AddRange(
                new TaskActivityLog { TaskId=seed.Task, UserId=seed.Owner, Action=ActivityLogAction.StatusChanged, OldValue="Todo", NewValue="Done", CreatedAt=from.AddHours(1) },
                new TaskActivityLog { TaskId=seed.Task, UserId=seed.Owner, Action=ActivityLogAction.StatusChanged, OldValue="Todo", NewValue="Done", CreatedAt=from.AddDays(2) },
                new TaskActivityLog { TaskId=invalidDuration.Id, UserId=seed.Owner, Action=ActivityLogAction.StatusChanged, OldValue="Todo", NewValue="Done", CreatedAt=from.AddDays(10) });
            await db.SaveChangesAsync();
        });
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(factory.ConnectionString).Options);
        var result = await new DashboardRepository(context).GetAnalyticsAsync(new DashboardScopeCriteria { UserId=seed.Owner },seed.Board,from,to);
        Assert.Equal(31,result.Burndown.Count);
        Assert.Equal(1,result.Burndown[0].Remaining);
        Assert.Equal(2,result.Burndown[1].Remaining);
        Assert.Equal(1,result.Burndown[2].Remaining);
        Assert.Equal(2,result.AvgCompletionDays); // (4 days + max(0, -10 days)) / 2
        Assert.Equal(1,result.Velocity.Single(p=>p.Period=="2026-01-01").Completed);
        Assert.Equal(1,result.Velocity.Single(p=>p.Period=="2026-01-03").Completed);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [SqlFact]
    public async Task AggregatesAndDeepActivityExecuteOnSqlServer()
    {
        await using var factory=new Phase2SqlFactory();
        await factory.InitializeAsync();
        var seed=await factory.SeedAsync();
        var now=DateTime.UtcNow;
        await factory.WithDb(async db => {
            for(var i=0;i<220;i++) db.TaskActivityLogs.Add(new TaskActivityLog { TaskId=seed.Task, UserId=seed.Owner,
                Action=ActivityLogAction.StatusChanged, OldValue="Todo", NewValue="Done", CreatedAt=now.AddMinutes(-i) });
            db.TimeEntries.Add(new TimeEntry { TaskId=seed.Task, UserId=seed.Owner, StartTime=now.AddHours(-1), EndTime=now, DurationSeconds=3600, IsBillable=true });
            db.TimeEntries.Add(new TimeEntry { TaskId=seed.Task, UserId=seed.Owner, StartTime=now.AddMinutes(-2) });
            await db.SaveChangesAsync();
        });
        var counter=new QueryCounter();
        await using var context=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(factory.ConnectionString).AddInterceptors(counter).Options);
        var repository=new DashboardRepository(context);
        var scope=new DashboardScopeCriteria { UserId=seed.Owner };
        Assert.Equal(1,(await repository.GetStatsAsync(scope)).Total);
        Assert.Equal(2,counter.Commands.Count);
        counter.Commands.Clear();
        var velocity=await repository.GetVelocityAsync(scope,DashboardTimeframe.Month,now);
        Assert.Equal(1,velocity.Single(p => p.Start.Date==now.Date).Completed);
        var page=await repository.GetActivityAsync(scope,20,10);
        Assert.Equal(10,page.Items.Count);
        Assert.Equal(221,page.TotalItems);
        output.WriteLine("Activity SQL: "+string.Join("\n",counter.Commands));
        var time=new TimeTrackingRepository(context);
        var query=new TimeQueryDto { From=now.AddDays(-1), To=now.AddSeconds(1), PageSize=1 };
        var history=await time.GetUserPageAsync(seed.Owner,false,query,default);
        Assert.Equal(2,history.TotalItems);
        Assert.InRange(history.Items.Single().DurationSeconds,110,180);
        var report=await time.GetReportAsync(seed.Owner,false,query,default);
        Assert.Equal(3600,report.TotalSeconds);
        Assert.Equal(1,report.EntryCount);
        Assert.Equal(3600,report.ByTask.Single().TotalSeconds);
        Assert.Equal(0,(await time.GetReportAsync(seed.Outsider,false,query,default)).EntryCount);
        var analytics=await repository.GetAnalyticsAsync(scope,seed.Board,now.AddDays(-1),now.AddSeconds(1));
        Assert.Equal(1,analytics.TotalTasks);
        Assert.Equal(1,analytics.Velocity.Where(x => x.Period==now.ToString("yyyy-MM-dd")).Sum(x=>x.Completed));
        Assert.Equal(3600,analytics.TotalTimeTrackedSeconds);
        var empty=await repository.GetAnalyticsAsync(scope,Guid.NewGuid(),now.AddDays(-1),now.AddSeconds(1));
        Assert.Equal(0,empty.TotalTasks);
        Assert.All(empty.Velocity,x=>Assert.Equal(0,x.Completed));
        Assert.Equal(seed.Owner,(await new UserRepository(context).GetByEmailAsync(" OWNER@EXAMPLE.INVALID "))!.Id);
        var collation=await context.Database.SqlQueryRaw<string>("SELECT CAST(DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS nvarchar(128)) AS Value").SingleAsync();
        output.WriteLine("Disposable database collation: "+collation);
    }
}
