using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskHub.Infrastructure.Data;
using Xunit;

namespace TaskHub.Tests.Fixtures;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "SQL RUNTIME VERIFICATION REQUIRED: Windows SQL LocalDB required.";
    }
}

public sealed class Phase2SqlFactory : Phase1ApiFactory
{
    // Never load the application's connection string for destructive tests.
    private readonly string _database = "TaskHub_Phase2_Test_" + Guid.NewGuid().ToString("N");
    public SaveFailure Failure { get; } = new();
    public Action<IServiceCollection>? ConfigureServices { get; set; }
    public string ConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = _database,
        IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 15
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(ConnectionString).AddInterceptors(Failure));
            ConfigureServices?.Invoke(services);
        });
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task WithDb(Func<AppDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_database.StartsWith("TaskHub_Phase2_Test_", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe test database name.");
        Failure.EntityName = null;
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options))
            await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}

public sealed class SaveFailure : SaveChangesInterceptor
{
    public string? EntityName { get; set; }
    public EntityState State { get; set; } = EntityState.Added;
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (EntityName != null && eventData.Context!.ChangeTracker.Entries()
            .Any(e => e.State == State && e.Entity.GetType().Name == EntityName))
            throw new InvalidOperationException("Injected persistence failure.");
        return ValueTask.FromResult(result);
    }
}
