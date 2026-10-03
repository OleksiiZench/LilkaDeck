using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LilkaDeckApp.Protocol;

namespace LilkaDeckApp.Transport;

/// <summary>
/// Serial port access with a single reader: one background thread moves incoming bytes into a queue,
/// and both lines and binary blocks are taken from that queue in order.
/// </summary>
public sealed class SerialPortTransport : ISerialTransport
{
    private const int PollTimeoutMs = 100;
    private const int WriteTimeoutMs = 3000;
    private const int ReadBufferSize = 1024;

    private readonly SerialPort _port;
    private readonly Channel<byte[]> _incoming = Channel.CreateUnbounded<byte[]>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    private readonly ChunkReader _reader;
    private readonly CancellationTokenSource _stop = new();
    private readonly object _writeLock = new();
    private Task? _readLoop;

    public SerialPortTransport(string portName)
    {
        // Asserting DTR and RTS while opening also resets the ESP32, so the device needs a moment to boot.
        _port = new SerialPort(portName, LilkaProtocol.BaudRate)
        {
            ReadTimeout = PollTimeoutMs,
            WriteTimeout = WriteTimeoutMs,
            DtrEnable = true,
            RtsEnable = true
        };
        _reader = new ChunkReader(_incoming.Reader);
    }

    public string PortName => _port.PortName;

    public void Open()
    {
        _port.Open();
        _readLoop = Task.Factory.StartNew(ReadLoop, TaskCreationOptions.LongRunning);
    }

    public Task WriteLineAsync(string line, CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
        return WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    public Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            lock (_writeLock)
            {
                _port.Write(data, offset, count);
            }
        }, cancellationToken);

    public Task<string> ReadLineAsync(CancellationToken cancellationToken) =>
        _reader.ReadLineAsync(cancellationToken);

    public Task<byte[]> ReadExactAsync(int count, CancellationToken cancellationToken) =>
        _reader.ReadExactAsync(count, cancellationToken);

    public void Dispose()
    {
        _stop.Cancel();
        try
        {
            _port.Close();
        }
        catch (Exception)
        {
            // The port may already be gone because the device was unplugged.
        }

        _readLoop?.Wait(PollTimeoutMs * 5);
        _port.Dispose();
        _stop.Dispose();
    }

    private void ReadLoop()
    {
        var buffer = new byte[ReadBufferSize];
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                int count = ReadAvailable(buffer);
                if (count > 0)
                {
                    _incoming.Writer.TryWrite(buffer.AsSpan(0, count).ToArray());
                }
            }
            _incoming.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            // Unplugging the device surfaces here and fails every pending read.
            _incoming.Writer.TryComplete(_stop.IsCancellationRequested ? null : ex);
        }
    }

    private int ReadAvailable(byte[] buffer)
    {
        try
        {
            return _port.Read(buffer, 0, buffer.Length);
        }
        catch (TimeoutException)
        {
            return 0;
        }
    }
}
