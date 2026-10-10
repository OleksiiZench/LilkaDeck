using System;
using System.Threading.Tasks;
using LilkaDeckApp.Device;
using LilkaDeckApp.Services;

namespace LilkaDeckApp.ViewModels;

/// <summary>
/// Turns what happens to the connection into what the user sees: the status, the log, the profile read
/// after connecting, and the programs the device asks to launch. The events come from a background
/// thread, so every change of a view model is posted to the UI thread.
/// </summary>
public sealed class DeviceEventPresenter : IDisposable
{
    private readonly IDeviceEvents _events;
    private readonly IUiDispatcher _ui;
    private readonly ActivityViewModel _activity;
    private readonly ConnectionViewModel _connection;
    private readonly FailureReporter _failures;
    private readonly LaunchHandler _launcher;
    private readonly Func<Task> _refreshProfile;
    
    public DeviceEventPresenter(
        IDeviceEvents events,
        IUiDispatcher ui,
        ActivityViewModel activity,
        ConnectionViewModel connection,
        FailureReporter failures,
        LaunchHandler launcher,
        Func<Task> refreshProfile)
    {
        _events = events;
        _ui = ui;
        _activity = activity;
        _connection = connection;
        _failures = failures;
        _launcher = launcher;
        _refreshProfile = refreshProfile;
        
        events.Connected += OnConnected;
        events.Disconnected += OnDisconnected;
        events.Failed += failures.Report;
        events.LogReceived += OnLogReceived;
        events.ExecuteRequested += OnExecuteRequested;
    }
    
    public void Dispose()
    {
        _events.Connected -= OnConnected;
        _events.Disconnected -= OnDisconnected;
        _events.Failed -= _failures.Report;
        _events.LogReceived -= OnLogReceived;
        _events.ExecuteRequested -= OnExecuteRequested;
    }
    
    private void OnConnected(string portName) => _ui.Post(() =>
    {
        _connection.SetConnected(portName);
        _activity.Info($"Підключено до порту {portName}");
        _activity.Info("Завантаження конфігурації з Лілки...");
        _ = RefreshProfileAsync();
    });
    
    private void OnDisconnected() => _ui.Post(() =>
    {
        _connection.SetSearching();
        _activity.Error("Пристрій відключено. Пошук...");
    });
    
    private void OnLogReceived(string text) => _ui.Post(() => _activity.Info($"[ESP32] {text}"));
    
    private void OnExecuteRequested(string target) => _ui.Post(() =>
    {
        if (!_launcher.TryLaunch(target, out string? error)) _activity.Error($"Launch error: {error}");
    });
    
    private async Task RefreshProfileAsync()
    {
        try
        {
            await _refreshProfile();
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
