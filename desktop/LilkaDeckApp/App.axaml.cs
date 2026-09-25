using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace LilkaDeckApp;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            _mainWindow = mainWindow;
            desktop.MainWindow = mainWindow;

            desktop.ShutdownRequested += (sender, e) =>
            {
                mainWindow.IsRealClose = true;
            };

            Program.ShowWindowAction = () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    mainWindow.Show();
                    if (mainWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                    {
                        mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                    }
                    mainWindow.Activate();
                    mainWindow.Topmost = true;
                    mainWindow.Topmost = false;
                });
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        ToggleWindowVisibility();
    }

    private void ToggleWindow_Clicked(object? sender, EventArgs e)
    {
        ToggleWindowVisibility();
    }

    private void Exit_Clicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_mainWindow != null)
            {
                _mainWindow.IsRealClose = true;
            }
            desktop.Shutdown();
        }
    }

    private void ToggleWindowVisibility()
    {
        if (_mainWindow == null) return;

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }
}
