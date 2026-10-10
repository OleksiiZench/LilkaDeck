using System;
using LilkaDeckApp.Services;

namespace LilkaDeckApp.ViewModels;

/// <summary>Shows a failure in the connection status and in the log. Safe to call from any thread.</summary>
public sealed class FailureReporter
{
    private readonly IUiDispatcher _ui;
    private readonly ActivityViewModel _activity;
    private readonly ConnectionViewModel _connection;
    
    public FailureReporter(IUiDispatcher ui, ActivityViewModel activity, ConnectionViewModel connection)
    {
        _ui = ui;
        _activity = activity;
        _connection = connection;
    }
    
    public void Report(Exception exception)
    {
        string text = DeviceErrorText.Describe(exception);
        _ui.Post(() =>
        {
            _connection.SetError(text);
            _activity.Error($"Синхронізацію перервано: {text}");
        });
    }
}
