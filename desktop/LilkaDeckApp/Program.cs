using Avalonia;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LilkaDeckApp;

class Program
{

    [DllImport("libc", SetLastError = true)]
    private static extern void _exit(int status);

    private static FileStream? _lockFile;
    public static Action? ShowWindowAction;
    private static int _shuttingDown = 0;

    private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Shutdown();

        PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx =>
        {
            ctx.Cancel = true;
            Shutdown();
        });
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx =>
        {
            ctx.Cancel = true;
            Shutdown();
        });

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
            return;
        }

        StartIpcServer();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Shutdown();
        }
    }

    private static void Shutdown()
    {
        if (Interlocked.Exchange(ref _shuttingDown, 1) != 0) return;

        _cts.Cancel();
        _lockFile?.Dispose();

        _exit(0);
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
        catch (Exception) { }
    }

    private static void StartIpcServer()
    {
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream("LilkaDeck_IPC_Pipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(_cts.Token);

                    using var reader = new StreamReader(server);
                    string? msg = await reader.ReadLineAsync(_cts.Token);

                    if (msg == "SHOW")
                    {
                        ShowWindowAction?.Invoke();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                }
            }
        }, _cts.Token);
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
