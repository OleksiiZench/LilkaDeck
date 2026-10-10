using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LilkaDeckApp.Transport;

namespace LilkaDeckApp.Device;

public sealed record ConnectionTiming
{
    public TimeSpan ScanInterval { get; init; } = TimeSpan.FromSeconds(2);

    // Opening the port resets the ESP32, so it needs a moment before it can answer.
    public TimeSpan BootDelay { get; init; } = TimeSpan.FromSeconds(1.5);

    public TimeSpan PingTimeout { get; init; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Looks for the Lilka on the available ports and, once connected, checks every few seconds that it still answers.
/// </summary>
public sealed class ConnectionMonitor : IDisposable
{
    private readonly Func<string[]> _listPorts;
    private readonly Func<string, ISerialTransport> _createTransport;
    private readonly ConnectionTiming _timing;
    private CancellationTokenSource? _cts;
    private Task _loop = Task.CompletedTask;
    private DeviceSession? _session;

    public ConnectionMonitor(
        Func<string[]> listPorts, Func<string, ISerialTransport> createTransport, ConnectionTiming? timing = null)
    {
        _listPorts = listPorts;
        _createTransport = createTransport;
        _timing = timing ?? new ConnectionTiming();
    }

    /// <summary>Raised for every port that is tried, before the session starts, so listeners can attach to it.</summary>
    public event Action<DeviceSession>? SessionCreated;
    public event Action<string>? Connected;

    /// <summary>The failure that ended the connection, or null when it simply stopped answering.</summary>
    public event Action<Exception?>? Disconnected;

    public DeviceSession? Session => Volatile.Read(ref _session);
    public bool IsConnected => Session is { IsClosed: false };

    public void Start()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _loop = RunAsync(_cts.Token);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _loop.Wait(TimeSpan.FromSeconds(2));

        var session = Session;
        if (session != null) Drop(session, null, notify: false);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await RunOnceAsync(cancellationToken);
                await Task.Delay(_timing.ScanInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped or restarted.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var session = Session;
            if (session == null) await ConnectToAnyPortAsync(cancellationToken);
            else await CheckHeartbeatAsync(session, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Trace.TraceWarning($"Connection monitor iteration failed: {ex.Message}");
        }
    }

    private async Task ConnectToAnyPortAsync(CancellationToken cancellationToken)
    {
        foreach (string port in _listPorts())
        {
            var session = await TryOpenAsync(port, cancellationToken);
            if (session == null) continue;

            Volatile.Write(ref _session, session);
            Raise(Connected, port);
            return;
        }
    }

    // Any failure here means "not our device": the port is busy, not accessible, or answers differently.
    private async Task<DeviceSession?> TryOpenAsync(string port, CancellationToken cancellationToken)
    {
        ISerialTransport? transport = null;
        DeviceSession? session = null;
        bool opened = false;
        try
        {
            transport = _createTransport(port);
            transport.Open();
            session = new DeviceSession(transport);
            Raise(SessionCreated, session);
            session.Start();

            await Task.Delay(_timing.BootDelay, cancellationToken);
            var answer = await new LilkaDeviceClient(session).PingAsync(_timing.PingTimeout, cancellationToken);
            opened = answer == PingResult.Alive;
            return opened ? session : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
        finally
        {
            if (!opened)
            {
                if (session != null) session.Dispose();
                else transport?.Dispose();
            }
        }
    }

    private async Task CheckHeartbeatAsync(DeviceSession session, CancellationToken cancellationToken)
    {
        if (!session.IsClosed && await IsAliveAsync(session, cancellationToken)) return;

        Drop(session, session.Failure, notify: true);
    }

    private async Task<bool> IsAliveAsync(DeviceSession session, CancellationToken cancellationToken)
    {
        try
        {
            return await new LilkaDeviceClient(session).PingAsync(_timing.PingTimeout, cancellationToken) != PingResult.NoAnswer;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    // Only the caller that actually removes the session reports it, so it is never reported twice.
    private void Drop(DeviceSession session, Exception? failure, bool notify)
    {
        if (Interlocked.CompareExchange(ref _session, null, session) != session) return;

        session.Dispose();
        if (notify) Raise(Disconnected, failure);
    }

    // A faulty listener must not stop the monitor.
    private static void Raise<T>(Action<T>? handler, T argument)
    {
        try
        {
            handler?.Invoke(argument);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"A connection event handler failed: {ex.Message}");
        }
    }
}
