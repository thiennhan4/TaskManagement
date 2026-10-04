using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public sealed class MutationRunner(AppDbContext db) : IMutationRunner
{
    private List<Func<Task>>? _afterCommit;
    private List<Func<Task>>? _onRollback;
    public void OnRollback(Func<Task> action) => (_onRollback ?? throw new InvalidOperationException("No mutation transaction.")).Add(action);

    public async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        if (_afterCommit != null) return await action();
        var callbacks = new List<Func<Task>>();
        _afterCommit = callbacks;
        _onRollback = new();
        try
        {
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
            T result;
            try
            {
                result = await action();
                await db.SaveChangesAsync(ct);
                if (transaction != null) await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try { await transaction.RollbackAsync(CancellationToken.None); }
                    catch (InvalidOperationException) { /* SQL Server already rolled back a deadlock victim. */ }
                    catch (SqlException) { /* Preserve the original persistence failure. */ }
                }
                if (transaction != null) await transaction.DisposeAsync();
                db.ChangeTracker.Clear();
                _afterCommit = null;
                foreach (var compensate in _onRollback) await compensate();
                if (IsConflict(ex))
                    throw new ConflictException("The resource changed concurrently or violates a uniqueness constraint. Reload and retry.");
                throw;
            }
            if (transaction != null) await transaction.DisposeAsync();
            _afterCommit = null;
            foreach (var callback in callbacks) await callback();
            return result;
        }
        finally { _afterCommit = null; _onRollback = null; }
    }

    private static bool IsConflict(Exception exception) => exception is DbUpdateConcurrencyException ||
        exception is SqlException { Number: 2601 or 2627 or 1205 } ||
        (exception.InnerException != null && IsConflict(exception.InnerException));

    public async Task RunAsync(Func<Task> action, CancellationToken ct = default) =>
        await RunAsync(async () => { await action(); return true; }, ct);

    public Task AfterCommitAsync(Func<Task> action)
    {
        if (_afterCommit == null) return action();
        _afterCommit.Add(action);
        return Task.CompletedTask;
    }
}
