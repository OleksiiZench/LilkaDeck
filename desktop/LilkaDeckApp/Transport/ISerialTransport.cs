using System;
using System.Threading;
using System.Threading.Tasks;

namespace LilkaDeckApp.Transport;

/// <summary>
/// A byte pipe to the device. Lines and binary blocks are read from the same ordered stream,
/// by one consumer at a time. Cancelling a read loses no data.
/// </summary>
public interface ISerialTransport : IDisposable
{
    string PortName { get; }

    void Open();

    Task WriteLineAsync(string line, CancellationToken cancellationToken);
    Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);

    /// <summary>Reads up to the next newline; the line comes back without its line ending.</summary>
    Task<string> ReadLineAsync(CancellationToken cancellationToken);

    Task<byte[]> ReadExactAsync(int count, CancellationToken cancellationToken);
}
