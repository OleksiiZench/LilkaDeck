using Avalonia;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace LilkaDeckApp;

class Program
{
    private static FileStream? _lockFile;

    public static Action? ShowWindowAction;

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
            SendShowSignal();
            Console.WriteLine("Відправлено сигнал розгортання першому процесу.");
            return;
        }

        StartIpcServer();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void SendShowSignal()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", "LilkaDeck_IPC_Pipe", PipeDirection.Out);
            client.Connect(500);
            using var writer = new StreamWriter(client);
            writer.WriteLine("SHOW");
        }
        catch (Exception)
        {
        }
    }

    private static void StartIpcServer()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream("LilkaDeck_IPC_Pipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync();

                    using var reader = new StreamReader(server);
                    string? msg = await reader.ReadLineAsync();

                    if (msg == "SHOW")
                    {
                        ShowWindowAction?.Invoke();
                    }
                }
                catch (Exception)
                {
                }
            }
        });
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
