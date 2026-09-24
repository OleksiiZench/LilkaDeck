using Avalonia;
using System;
using System.IO;

namespace LilkaDeckApp;

class Program
{
    private static FileStream? _lockFile;

    [STAThread]
    public static void Main(string[] args)
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string lockDir = Path.Combine(appDataPath, "LilkaDeck");

        Directory.CreateDirectory(lockDir);
        string lockFilePath = Path.Combine(lockDir, "single_instance.lock");

        try
        {
            _lockFile = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            Console.WriteLine("LilkaDeck вже запущено. Закриваємо дублікат.");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
