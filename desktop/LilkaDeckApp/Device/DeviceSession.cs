using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LilkaDeckApp.Protocol;
using LilkaDeckApp.Transport;

namespace LilkaDeckApp.Device;

/// <summary>The content of a file the device sent after FILE_SEND_START.</summary>
internal sealed record FileContentMessage(byte[] Data) : DeviceMessage;

/// <summary>
/// One live connection to the device. A single pump reads everything the device sends: log lines and
/// launch requests become events, and replies go to whoever is waiting for them. Requests are
/// serialized by an exchange lock, so a reply can only belong to the request in progress.
/// </summary>
public sealed class DeviceSession : IDisposable
{
    private static readonly TimeSpan PayloadStallTimeout = TimeSpan.FromSeconds(10);

    private readonly ISerialTransport _transport;
    private readonly SemaphoreSlim _exchangeLock = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private volatile Expectation? _pending;
    private volatile bool _closed;
    private Task _pump = Task.CompletedTask;

    public DeviceSession(ISerialTransport transport)
    {
        _transport = transport;
    }

    public event Action<string>? ExecuteRequested;
    public event Action<string>? LogReceived;

    public bool IsClosed => _closed;
    public Exception? Failure { get; private set; }

    public void Start() => _pump = Task.Run(PumpAsync);

    /// <summary>Waits for its turn, then runs the body as the only conversation with the device.</summary>
    public async Task<T> RunExclusiveAsync<T>(Func<Exchange, Task<T>> body, CancellationToken cancellationToken)
    {
        await _exchangeLock.WaitAsync(cancellationToken);
        try
        {
            return await body(new Exchange(this));
        }
        finally
        {
            _exchangeLock.Release();
        }
    }

    public Task RunExclusiveAsync(Func<Exchange, Task> body, CancellationToken cancellationToken) =>
        RunExclusiveAsync<bool>(async exchange =>
        {
            await body(exchange);
            return true;
        }, cancellationToken);

    /// <summary>Runs the body only if the device is free within the given time; otherwise returns false.</summary>
    public async Task<bool> TryRunExclusiveAsync(Func<Exchange, Task> body, TimeSpan wait, CancellationToken cancellationToken)
    {
        if (!await _exchangeLock.WaitAsync(wait, cancellationToken)) return false;
        try
        {
            await body(new Exchange(this));
            return true;
        }
        finally
        {
            _exchangeLock.Release();
        }
    }

    public void Dispose()
    {
        _lifetime.Cancel();
        _transport.Dispose();
        _pump.Wait(TimeSpan.FromSeconds(1));
        _lifetime.Dispose();
    }

    /// <summary>What a body may do with the device while it holds the exchange lock.</summary>
    public sealed class Exchange
    {
        private readonly DeviceSession _session;

        internal Exchange(DeviceSession session)
        {
            _session = session;
        }

        public Task SendLineAsync(string commandLine, CancellationToken cancellationToken)
        {
            _session.ThrowIfClosed();
            return _session._transport.WriteLineAsync(commandLine, cancellationToken);
        }

        public Task<DeviceMessage> RequestAsync(
            string commandLine, Func<DeviceMessage, bool> accepts, TimeSpan timeout, CancellationToken cancellationToken) =>
            _session.ExchangeAsync(
                () => _session._transport.WriteLineAsync(commandLine, cancellationToken), accepts, timeout, cancellationToken);

        public Task<DeviceMessage> SendBlockAsync(
            byte[] data, int offset, int count, Func<DeviceMessage, bool> accepts, TimeSpan timeout, CancellationToken cancellationToken) =>
            _session.ExchangeAsync(
                () => _session._transport.WriteAsync(data, offset, count, cancellationToken), accepts, timeout, cancellationToken);
    }

    // The expectation is registered before anything is sent, so even an instant reply cannot be missed.
    private async Task<DeviceMessage> ExchangeAsync(
        Func<Task> send, Func<DeviceMessage, bool> accepts, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var expectation = new Expectation(accepts);
        _pending = expectation;
        try
        {
            ThrowIfClosed();
            await send();
            return await expectation.Completion.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            _pending = null;
        }
    }

    private void ThrowIfClosed()
    {
        if (_closed) throw new TransportClosedException(Failure);
    }

    private async Task PumpAsync()
    {
        try
        {
            while (true)
            {
                string line = await _transport.ReadLineAsync(_lifetime.Token);
                var message = DeviceMessageParser.Parse(line);
                if (message != null) await RouteAsync(message);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // The session was disposed.
        }
        catch (Exception ex)
        {
            Failure = ex;
        }
        finally
        {
            _closed = true;
            _pending?.Fail(new TransportClosedException(Failure));
        }
    }

    private async Task RouteAsync(DeviceMessage message)
    {
        switch (message)
        {
            case ExecuteMessage execute:
                Raise(ExecuteRequested, execute.Target);
                break;
            case LogMessage log:
                Raise(LogReceived, log.Text);
                break;
            case FileSendStartMessage start:
                // The raw bytes belong to this message, so they are read here even if nobody is waiting for them.
                Deliver(new FileContentMessage(await ReadPayloadAsync(start.Size)));
                break;
            default:
                Deliver(message);
                break;
        }
    }

    private async Task<byte[]> ReadPayloadAsync(int size)
    {
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        stall.CancelAfter(PayloadStallTimeout);
        try
        {
            return await _transport.ReadExactAsync(size, stall.Token);
        }
        catch (OperationCanceledException) when (!_lifetime.IsCancellationRequested)
        {
            throw new IOException("The device stopped sending a file.");
        }
    }

    private void Deliver(DeviceMessage message)
    {
        var pending = _pending;
        if (pending == null || !pending.TryComplete(message))
        {
            Trace.TraceWarning($"Unexpected device message: {message}");
        }
    }

    // A slow or faulty listener must not take the connection down with it.
    private static void Raise(Action<string>? handler, string argument)
    {
        try
        {
            handler?.Invoke(argument);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"A device event handler failed: {ex.Message}");
        }
    }

    private sealed class Expectation
    {
        private readonly Func<DeviceMessage, bool> _accepts;
        private readonly TaskCompletionSource<DeviceMessage> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Expectation(Func<DeviceMessage, bool> accepts)
        {
            _accepts = accepts;
        }

        public Task<DeviceMessage> Completion => _completion.Task;

        public bool TryComplete(DeviceMessage message) => _accepts(message) && _completion.TrySetResult(message);

        public void Fail(Exception exception) => _completion.TrySetException(exception);
    }
}
