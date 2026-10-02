using System.Data.Common;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using TaskHub.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace TaskHub.Tests.Integration;

public class Phase3QueryEvidenceTests(ITestOutputHelper output)
{
    [SqlFact]
    public async Task QueryEvidence_10_100_1000_Cards()
    {
        foreach (var size in new[] { 10, 100, 1000 })
        {
            await using var factory = new Phase2SqlFactory();
            await factory.InitializeAsync();
            var seed = await factory.SeedAsync();
            await factory.WithDb(async db =>
            {
                for (var i = 1; i < size; i++)
                    db.Tasks.Add(new TaskItem { ListId = seed.List, OwnerId = seed.Owner, Title = "Card " + i, Position = i });
                await db.SaveChangesAsync();
                var ids = await db.Tasks.Where(t => !t.IsDeleted).Select(t => t.Id).ToListAsync();
                foreach (var id in ids)
                {
                    db.Comments.Add(new Comment { TaskId = id, UserId = seed.Owner, Content = "Comment" });
                    db.TaskAttachments.Add(new TaskAttachment { TaskId = id, UploadedByUserId = seed.Owner, FileName = "test.txt", FilePath = "synthetic.txt", ContentType = "text/plain" });
                    db.TaskActivityLogs.Add(new TaskActivityLog { TaskId = id, UserId = seed.Owner, Action = ActivityLogAction.Created });
                }
                await db.SaveChangesAsync();
            });
            var counter = new QueryCounter();
            await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(factory.ConnectionString).AddInterceptors(counter).Options);
            // Reproduce the committed Phase 1 graph for a same-data allocation sample.
            // Process allocation deltas include EF compilation/background work, not just DTO memory.
            var allocated = GC.GetTotalAllocatedBytes(true);
            var legacy = await context.Boards.Include(b=>b.Project)
                .Include(b=>b.Lists).ThenInclude(l=>l.Tasks).ThenInclude(t=>t.Owner)
                .Include(b=>b.Lists).ThenInclude(l=>l.Tasks).ThenInclude(t=>t.AssignedTo)
                .Include(b=>b.Lists).ThenInclude(l=>l.Tasks).ThenInclude(t=>t.Comments)
                .Include(b=>b.Lists).ThenInclude(l=>l.Tasks).ThenInclude(t=>t.Attachments)
                .AsSplitQuery().SingleAsync(b=>b.Id==seed.Board);
            output.WriteLine($"ALLOCATION size={size} shape=legacyGraph processBytes={GC.GetTotalAllocatedBytes(true)-allocated} returnedCards={legacy.Lists.Sum(l=>l.Tasks.Count)} tracked={context.ChangeTracker.Entries().Count()} queries={counter.Commands.Count}");
            context.ChangeTracker.Clear(); counter.Commands.Clear();
            allocated = GC.GetTotalAllocatedBytes(true);
            await new BoardRepository(context).GetBoardByIdAsync(seed.Board);
            output.WriteLine($"EVIDENCE size={size} boardQueries={counter.Commands.Count} boardTracked={context.ChangeTracker.Entries().Count()}");
            context.ChangeTracker.Clear(); counter.Commands.Clear();
            await new TaskItemRepository(context).GetByIdWithDetailsAsync(seed.Task);
            output.WriteLine($"EVIDENCE size={size} detailQueries={counter.Commands.Count} detailTracked={context.ChangeTracker.Entries().Count()}");
            context.ChangeTracker.Clear(); counter.Commands.Clear();
            var columns = await new BoardReadRepository(context).GetColumnsAsync(seed.Board, new KanbanQueryDto(), default);
            output.WriteLine($"EVIDENCE size={size} columnsQueries={counter.Commands.Count} columnsTracked={context.ChangeTracker.Entries().Count()}");
            var returnedCards = columns.Items.Sum(c=>c.Tasks.Items.Count);
            Assert.Equal(Math.Min(size,50),returnedCards);
            Assert.Equal(size,columns.Items.Single().Tasks.TotalItems);
            output.WriteLine($"ALLOCATION size={size} shape=boundedHeaderDetailColumns processBytes={GC.GetTotalAllocatedBytes(true)-allocated} returnedCards={returnedCards} sqlRows=not-instrumented");
            counter.Commands.Clear();
            var stats = await new DashboardRepository(context).GetStatsAsync(new DashboardScopeCriteria { UserId = seed.Owner });
            Assert.Equal(size, stats.Total);
            output.WriteLine($"EVIDENCE size={size} statsQueries={counter.Commands.Count}");
            if (size == 1000) await InspectIndexesAndPlan(factory.ConnectionString,seed);
        }
    }

    private async Task InspectIndexesAndPlan(string connectionString, SecuritySeed seed)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var catalog = connection.CreateCommand())
        {
            catalog.CommandText = "SELECT t.name, i.name, c.name, c.max_length, ic.key_ordinal FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.name IN ('Tasks','Lists','Comments','TaskActivityLogs','ProjectActivityLogs','Projects','Notifications','TimeEntries') ORDER BY t.name,i.name,ic.key_ordinal";
            await using var reader = await catalog.ExecuteReaderAsync();
            while (await reader.ReadAsync()) output.WriteLine($"INDEX table={reader.GetString(0)} index={reader.GetString(1)} column={reader.GetString(2)} maxBytes={reader.GetInt16(3)} ordinal={reader.GetByte(4)}");
        }
        foreach (var (name, sql) in new[] {
            ("cards", "SELECT Id,Title,Position FROM Tasks WHERE ListId=@list AND IsDeleted=0 ORDER BY Position,Id OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY"),
            ("comments", "SELECT Id,CreatedAt FROM Comments WHERE TaskId=@task AND IsDeleted=0 ORDER BY CreatedAt,Id OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY"),
            ("activity", "SELECT Id,CreatedAt FROM TaskActivityLogs WHERE TaskId=@task ORDER BY CreatedAt DESC,Id OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY"),
            ("projects", "SELECT Id,CreatedAt FROM Projects WHERE OwnerId=@user AND ArchivedAt IS NULL ORDER BY CreatedAt DESC,Id OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY"),
            ("notifications", "SELECT Id,CreatedAt FROM Notifications WHERE UserId=@user AND IsRead=0 ORDER BY CreatedAt DESC,Id OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY"),
            ("time", "SELECT TaskId,SUM(DurationSeconds) FROM TimeEntries WHERE UserId=@user AND StartTime>=DATEADD(day,-30,SYSUTCDATETIME()) GROUP BY TaskId")
        })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SET STATISTICS XML ON; " + sql + "; SET STATISTICS XML OFF;";
            command.Parameters.AddWithValue("@list",seed.List);
            command.Parameters.AddWithValue("@task",seed.Task);
            command.Parameters.AddWithValue("@user",seed.Owner);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = 0;
            while(await reader.ReadAsync()) rows++;
            Assert.True(await reader.NextResultAsync());
            Assert.True(await reader.ReadAsync());
            var plan = XDocument.Parse(reader.GetString(0));
            XNamespace ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";
            var operators = plan.Descendants(ns+"RelOp").Select(e=>(string?)e.Attribute("PhysicalOp")).Distinct();
            var indexes = plan.Descendants(ns+"Object").Select(e=>(string?)e.Attribute("Index")).Where(x=>x!=null).Distinct();
            output.WriteLine($"ACTUAL_PLAN probe={name} returnedSqlRows={rows} operators={string.Join(',',operators)} indexes={string.Join(',',indexes)} missingIndexGroups={plan.Descendants(ns+"MissingIndexGroup").Count()}");
        }
    }
}

public sealed class QueryCounter : DbCommandInterceptor
{
    public List<string> Commands { get; } = new();
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Commands.Add(command.CommandText); return ValueTask.FromResult(result); }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Commands.Add(command.CommandText); return ValueTask.FromResult(result); }
}
