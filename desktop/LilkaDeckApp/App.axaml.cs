using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LilkaDeckApp.Hosting;

namespace LilkaDeckApp;

public partial class App : Application
{
    private WindowController? _windowController;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;

            _windowController = new WindowController(mainWindow);
            desktop.ShutdownRequested += (_, _) => _windowController.AllowClose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Safe to call from any thread.</summary>
    public void ShowMainWindow() => Dispatcher.UIThread.Post(() => _windowController?.Show());

    /// <summary>Safe to call from any thread.</summary>
    public void RequestExit() => Dispatcher.UIThread.Post(() =>
    {
        _windowController?.AllowClose();
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    });

    private void TrayIcon_Clicked(object? sender, EventArgs e) => _windowController?.ToggleVisibility();

    private void ToggleWindow_Clicked(object? sender, EventArgs e) => _windowController?.ToggleVisibility();

    private void Exit_Clicked(object? sender, EventArgs e) => RequestExit();
}
