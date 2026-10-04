using Avalonia.Controls;

namespace LilkaDeckApp.Hosting;

/// <summary>
/// Owns how the main window appears and disappears. Closing it only hides it in the tray,
/// until <see cref="AllowClose"/> says the application is really exiting.
/// </summary>
public sealed class WindowController
{
    private readonly MainWindow _window;

    public WindowController(MainWindow window)
    {
        _window = window;
    }

    public void Show()
    {
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
        // Toggling Topmost brings the window to the front on desktops that refuse a plain activation.
        _window.Topmost = true;
        _window.Topmost = false;
    }

    public void ToggleVisibility()
    {
        if (_window.IsVisible) _window.Hide();
        else Show();
    }

    public void AllowClose() => _window.IsRealClose = true;
}
