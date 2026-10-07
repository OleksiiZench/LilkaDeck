using LilkaDeckApp.Mvvm;

namespace LilkaDeckApp.ViewModels;

public enum ConnectionState { Searching, Connected, Error }

/// <summary>Whether the Lilka is connected, in words and in the flags the view uses to color them.</summary>
public sealed class ConnectionViewModel : ObservableObject
{
    private ConnectionState _state = ConnectionState.Searching;
    private bool _isOnline;
    private string _detail = "";

    public ConnectionState State => _state;
    public bool IsConnected => _state == ConnectionState.Connected;
    public bool IsError => _state == ConnectionState.Error;

    /// <summary>True from the moment the device is found until it is lost. An error message does not change it.</summary>
    public bool IsOnline => _isOnline;

    public string StatusText => _state switch
    {
        ConnectionState.Connected => $"Статус: Підключено ({_detail})",
        ConnectionState.Error => $"Помилка: {_detail}",
        _ => "Статус: Пошук пристрою..."
    };

    public void SetSearching() => Change(ConnectionState.Searching, "", isOnline: false);

    public void SetConnected(string portName) => Change(ConnectionState.Connected, portName, isOnline: true);

    public void SetError(string message) => Change(ConnectionState.Error, message, _isOnline);

    private void Change(ConnectionState state, string detail, bool isOnline)
    {
        _state = state;
        _detail = detail;
        _isOnline = isOnline;

        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(IsError));
        OnPropertyChanged(nameof(IsOnline));
        OnPropertyChanged(nameof(StatusText));
    }
}
