using System;

namespace LilkaDeckApp.Device;

/// <summary>What happens to the connection, as events. They come from a background thread.</summary>
public interface IDeviceEvents
{
    event Action<string>? Connected;
    event Action? Disconnected;
    
    /// <summary>The connection broke for a reason other than the device being unplugged.</summary>
    event Action<Exception>? Failed;
    
    /// <summary>A log line the device sent, without any prefix.</summary>
    event Action<string>? LogReceived;
    
    /// <summary>The device asked this computer to launch a program or open a URL.</summary>
    event Action<string>? ExecuteRequested;
}
