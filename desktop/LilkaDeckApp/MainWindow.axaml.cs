using System;
using System.IO;
using System.IO.Ports;
using Avalonia.Controls;
using Avalonia.Input;
using LilkaDeckApp.Device;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.Transport;

namespace LilkaDeckApp;

public partial class MainWindow : Window
{
    private readonly DeviceConnection _device;
    private readonly DeckEditSession _session;
    
    public bool IsRealClose { get; set; } = false;
    
    public MainWindow()
    {
        InitializeComponent();
        
        AddHandler(DragDrop.DropEvent, OnDrop);
        
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var caches = new ProfileCaches(Path.Combine(appData, "LilkaDeck", "Cache"));
        _session = new DeckEditSession(caches);
        _device = new DeviceConnection(new ConnectionMonitor(SerialPort.GetPortNames, port => new SerialPortTransport(port)));
        
        InitializeShell(caches);
        
        _device.Connected += HandleConnected;
        _device.Disconnected += HandleDisconnected;
        _device.ExecuteRequested += HandleExecuteRequest;
        _device.LogReceived += HandleLogMessage;
        _device.Failed += HandleError;
        
        _device.Start();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!IsRealClose)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }
}
