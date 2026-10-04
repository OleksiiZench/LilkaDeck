using System;
using Avalonia;
using LilkaDeckApp.Hosting;

namespace LilkaDeckApp;

internal static class Program
{
    private static readonly TimeSpan ShutdownGracePeriod = TimeSpan.FromSeconds(3);

    [STAThread]
    public static int Main(string[] args)
    {
        using var instance = SingleInstanceGuard.TryAcquire();
        if (instance == null)
        {
            ShowSignalPipe.NotifyRunningInstance();
            return 0;
        }

        using var signals = new ShutdownSignals(OnTerminationSignal);
        using var pipe = ShowSignalPipe.Listen(ShowMainWindow);

        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // Normally the process ends on its own right after this; the watchdog covers the case where it does not.
            ExitWatchdog.Arm(ShutdownGracePeriod);
        }
    }

    private static void ShowMainWindow() => (Application.Current as App)?.ShowMainWindow();

    // The UI thread may be stuck, or the window hidden in the tray, so a watchdog backs up the normal shutdown.
    private static void OnTerminationSignal()
    {
        ExitWatchdog.Arm(ShutdownGracePeriod);

        if (Application.Current is App app) app.RequestExit();
        else Environment.Exit(0);
    }

    public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
    .UsePlatformDetect()
#if DEBUG
    .WithDeveloperTools()
#endif
    .WithInterFont()
    .LogToTrace();
}
