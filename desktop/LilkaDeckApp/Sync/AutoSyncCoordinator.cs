using System;
using System.Threading;
using System.Threading.Tasks;

namespace LilkaDeckApp.Sync;

/// <summary>
/// Decides when the editor is saved to the device. Changes are collected for a short moment and sent together;
/// changes made while a save is running cause exactly one more save afterwards.
/// </summary>
public sealed class AutoSyncCoordinator
{
    private readonly Func<Task> _syncOnce;
    private readonly Action<Exception> _onError;
    private readonly TimeSpan _debounce;
    private readonly object _gate = new();
    private CancellationTokenSource? _debounceCts;
    private bool _running;
    private bool _pending;

    public AutoSyncCoordinator(Func<Task> syncOnce, Action<Exception> onError, TimeSpan debounce)
    {
        _syncOnce = syncOnce;
        _onError = onError;
        _debounce = debounce;
    }

    /// <summary>Asks for a save soon; each new request restarts the wait.</summary>
    public void RequestSync()
    {
        var wait = new CancellationTokenSource();
        lock (_gate)
        {
            _debounceCts?.Cancel();
            _debounceCts = wait;
        }
        _ = WaitThenSyncAsync(wait.Token);
    }

    /// <summary>Saves right away. If a save is already running, one more follows it and this returns at once.</summary>
    public async Task SyncNowAsync()
    {
        lock (_gate)
        {
            _debounceCts?.Cancel();
            _debounceCts = null;

            if (_running)
            {
                _pending = true;
                return;
            }
            _running = true;
        }

        try
        {
            await RunPassesAsync();
        }
        finally
        {
            lock (_gate) { _running = false; }
        }
    }

    private async Task WaitThenSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounce, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        await SyncNowAsync();
    }

    // Looking at the pending flag and giving up the "running" state happen together, so a request can never be lost.
    private async Task RunPassesAsync()
    {
        while (true)
        {
            lock (_gate) { _pending = false; }

            try
            {
                await _syncOnce();
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }

            lock (_gate)
            {
                if (!_pending)
                {
                    _running = false;
                    return;
                }
            }
        }
    }

    private void ReportError(Exception exception)
    {
        try
        {
            _onError(exception);
        }
        catch (Exception)
        {
            // A failing error handler must not leave the coordinator stuck.
        }
    }
}
