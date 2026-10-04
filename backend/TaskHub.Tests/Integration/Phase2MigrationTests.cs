using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskHub.Domain.Entities;
using TaskHub.Tests.Fixtures;
using Xunit;
namespace TaskHub.Tests.Integration;

public class Phase2MigrationTests
{
    [SqlFact]
    public async Task LegacyTeamType_NormalizesAndUnknownTypeBlocksWithoutPartialSchema()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.WithDb(db => db.GetService<IMigrator>().MigrateAsync("20260904055118_AddProjectTypeEnumAndNullableWorkspace"));
        await factory.SeedTeamAsync();
        await factory.WithDb(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE Projects SET ProjectType = N'unknown'");
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.MigrateAsync());
            Assert.DoesNotContain("20260925043942_AddMutationOutboxAndScopedUniqueness", await db.Database.GetAppliedMigrationsAsync());
            await db.Database.ExecuteSqlRawAsync("UPDATE Projects SET ProjectType = N'1'");
            Assert.Equal(0, await db.Projects.CountAsync(p => p.ProjectType == ProjectType.Team));
            await db.Database.MigrateAsync();
            Assert.Equal(1, await db.Projects.CountAsync(p => p.ProjectType == ProjectType.Team));
        });
    }

    [SqlFact]
    public async Task HistoricalChain_LegacyProjectType_RehearsalAndNewMigration()
    {
        await using var factory = new Phase2SqlFactory();
        await factory.WithDb(db => db.GetService<IMigrator>().MigrateAsync("20260904055118_AddProjectTypeEnumAndNullableWorkspace"));
        var seed = await factory.SeedAsync();
        await factory.WithDb(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE Projects SET ProjectType = N'0'");
            db.ChangeTracker.Clear();
            // Numeric strings materialize but fail SQL predicates using enum names.
            Assert.Equal(ProjectType.Personal, (await db.Projects.SingleAsync()).ProjectType);
            Assert.Equal(0, await db.Projects.CountAsync(p => p.ProjectType == ProjectType.Personal));
            await db.Database.MigrateAsync();
            Assert.Equal(1, await db.Projects.CountAsync(p => p.ProjectType == ProjectType.Personal));
            Assert.Empty(await db.OutboxMessages.ToListAsync());
        });
    }
}
