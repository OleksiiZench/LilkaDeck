using System;
using System.IO;

namespace LilkaDeckApp.Transport;

/// <summary>The connection ended: the device was unplugged or the port was closed.</summary>
public sealed class TransportClosedException : IOException
{
    public TransportClosedException(Exception? inner)
        : base("The serial connection was closed.", inner)
    {
    }
}
