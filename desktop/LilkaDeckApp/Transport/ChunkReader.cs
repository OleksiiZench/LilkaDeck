using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LilkaDeckApp.Transport;

/// <summary>
/// Reads lines and fixed-size blocks from a queue of received chunks. Bytes stay in one ordered
/// buffer, so a block that follows a line is never lost, and a cancelled read consumes nothing.
/// Not thread-safe: use one consumer at a time.
/// </summary>
public sealed class ChunkReader
{
    private const int MaxLineLength = 4096;

    private readonly ChannelReader<byte[]> _chunks;
    private byte[] _pending = Array.Empty<byte>();

    public ChunkReader(ChannelReader<byte[]> chunks)
    {
        _chunks = chunks;
    }

    public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
    {
        int searchFrom = 0;
        while (true)
        {
            int newline = Array.IndexOf(_pending, (byte)'\n', searchFrom);
            if (newline >= 0) return TakeLine(newline);

            if (_pending.Length > MaxLineLength)
            {
                throw new InvalidDataException($"No line ending within {MaxLineLength} bytes.");
            }

            searchFrom = _pending.Length;
            await AppendNextChunkAsync(cancellationToken);
        }
    }

    public async Task<byte[]> ReadExactAsync(int count, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        while (_pending.Length < count)
        {
            await AppendNextChunkAsync(cancellationToken);
        }

        var block = _pending.AsSpan(0, count).ToArray();
        Consume(count);
        return block;
    }

    private string TakeLine(int newlineIndex)
    {
        bool hasCarriageReturn = newlineIndex > 0 && _pending[newlineIndex - 1] == (byte)'\r';
        int length = hasCarriageReturn ? newlineIndex - 1 : newlineIndex;

        string line = Encoding.UTF8.GetString(_pending, 0, length);
        Consume(newlineIndex + 1);
        return line;
    }

    private void Consume(int count)
    {
        _pending = _pending.AsSpan(count).ToArray();
    }

    private async Task AppendNextChunkAsync(CancellationToken cancellationToken)
    {
        byte[] chunk;
        try
        {
            chunk = await _chunks.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException ex)
        {
            throw new TransportClosedException(ex.InnerException);
        }

        var merged = new byte[_pending.Length + chunk.Length];
        Buffer.BlockCopy(_pending, 0, merged, 0, _pending.Length);
        Buffer.BlockCopy(chunk, 0, merged, _pending.Length, chunk.Length);
        _pending = merged;
    }
}
