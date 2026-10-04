namespace TaskHub.Application.Repositories.Interfaces;

/// <summary>One business transaction, including nested repository saves and durable delivery intent.</summary>
public interface IMutationRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
    Task RunAsync(Func<Task> action, CancellationToken ct = default);
    void OnRollback(Func<Task> action);
    Task AfterCommitAsync(Func<Task> action);
}
