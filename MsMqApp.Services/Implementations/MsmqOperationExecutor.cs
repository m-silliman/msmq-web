using System.Collections.Concurrent;
using MsMqApp.Services.Helpers;

namespace MsMqApp.Services.Implementations;

/// <summary>
/// Shared across circuits. Deadlines bound the caller's wait, not the native RPC.
/// An abandoned worker retains its permits until the native call actually returns.
/// </summary>
public sealed class MsmqOperationExecutor
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _servers = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _global;
    private readonly int _perServer;

    public MsmqOperationExecutor(int perServer = 2, int global = 8)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(perServer, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(global, 1);
        _perServer = perServer;
        _global = new SemaphoreSlim(global, global);
    }

    public async Task<T> ExecuteAsync<T>(string address, Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        var token = deadline.Token;
        var server = _servers.GetOrAdd(MsmqAddress.Server(address), _ => new SemaphoreSlim(_perServer, _perServer));
        try
        {
            await server.WaitAsync(token).ConfigureAwait(false);
            try { await _global.WaitAsync(token).ConfigureAwait(false); }
            catch { server.Release(); throw; }

            // Do not pass token to Task.Run: the worker owns both permits even when canceled
            // between admission and dispatch, and must release them in its finally block.
            var worker = Task.Run(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    return await operation(token).ConfigureAwait(false);
                }
                finally
                {
                    _global.Release();
                    server.Release();
                }
            });
            _ = worker.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return await worker.WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("MSMQ operation timed out. A native call may still be running; " +
                "if this operation changes messages, its outcome is unknown. Refresh before retrying.");
        }
    }
}
