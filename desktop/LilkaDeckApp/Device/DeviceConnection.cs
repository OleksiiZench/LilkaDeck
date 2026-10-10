using System;
using LilkaDeckApp.Transport;

namespace LilkaDeckApp.Device;

/// <summary>
/// The connection to the Lilka as the rest of the application sees it: a few events and the device that is
/// connected at the moment. Events come from a background thread.
/// </summary>
public sealed class DeviceConnection : IDeviceSource, IDeviceEvents, IDisposable
{
    private readonly ConnectionMonitor _monitor;
    private readonly DeviceTimeouts? _timeouts;
    private volatile LilkaDeviceClient? _client;
    
    public DeviceConnection(ConnectionMonitor monitor, DeviceTimeouts? timeouts = null)
    {
        _monitor = monitor;
        _timeouts = timeouts;
        
        _monitor.SessionCreated += AttachToSession;
        _monitor.Connected += HandleConnected;
        _monitor.Disconnected += HandleDisconnected;
    }
    
    public event Action<string>? Connected;
    public event Action? Disconnected;
    public event Action<Exception>? Failed;
    public event Action<string>? LogReceived;
    public event Action<string>? ExecuteRequested;
    
    public bool IsConnected => _monitor.IsConnected;
    
    public ILilkaDevice? Current => IsConnected ? _client : null;
    
    /// <summary>Starts looking for the device.</summary>
    public void Start() => _monitor.Start();
    
    public void Dispose() => _monitor.Dispose();
    
    private void AttachToSession(DeviceSession session)
    {
        session.LogReceived += text => LogReceived?.Invoke(text);
        session.ExecuteRequested += target => ExecuteRequested?.Invoke(target);
    }
    
    private void HandleConnected(string portName)
    {
        _client = new LilkaDeviceClient(_monitor.Session!, _timeouts);
        Connected?.Invoke(portName);
    }
    
    private void HandleDisconnected(Exception? failure)
    {
        _client = null;
        Disconnected?.Invoke();
        if (failure != null && failure is not TransportClosedException) Failed?.Invoke(failure);
    }
}
