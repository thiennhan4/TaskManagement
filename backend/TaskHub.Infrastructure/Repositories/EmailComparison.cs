using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

internal static class EmailComparison
{
    // Inspect the column, not just the database default: deployments may override it.
    // Case-sensitive/unknown schemas retain the compatibility predicate until reviewed.
    public static async Task<bool> IsCaseInsensitiveAsync(AppDbContext db, string table, string column, CancellationToken ct)
    {
        if (!db.Database.IsRelational()) return false;
        return await db.Database.SqlQuery<int>($"SELECT CAST(COLLATIONPROPERTY(c.collation_name, 'ComparisonStyle') AS int) AS Value FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id WHERE t.name={table} AND c.name={column}")
            .Select(style => (style & 1) == 1).SingleOrDefaultAsync(ct);
    }
}
