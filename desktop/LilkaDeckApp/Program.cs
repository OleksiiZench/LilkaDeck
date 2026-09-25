using Avalonia;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace LilkaDeckApp;

class Program
{
    private static FileStream? _lockFile;
    public static Action? ShowWindowAction;

    // Додаємо токен скасування для керування фоновим процесом
    private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

    [STAThread]
    public static void Main(string[] args)
    {
        // Перехоплюємо сигнал вимкнення від Linux (SIGTERM) або закриття через термінал (Ctrl+C)
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Shutdown();
        Console.CancelKeyPress += (sender, e) => Shutdown();

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
            // Ми дублікат. Відправляємо сигнал і закриваємось.
            SendShowSignal();
            return;
        }

        // Передаємо токен у фоновий потік
        StartIpcServer();

        try
        {
            // Запускаємо інтерфейс
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // Якщо вікно закрили хрестиком, також коректно прибираємо за собою
            Shutdown();
        }
    }

    // Метод для миттєвого та чистого звільнення ресурсів
    private static void Shutdown()
    {
        _cts.Cancel();       // Зупиняємо цикл IPC
        _lockFile?.Dispose(); // Знімаємо блокування з файлу
        Environment.Exit(0);  // Миттєво повідомляємо Linux, що ми завершили роботу
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
            // Цикл працює, поки не надійде сигнал скасування (_cts.Cancel)
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream("LilkaDeck_IPC_Pipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    // Тепер сервер не зависає назавжди, а може бути перерваний токеном
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
                    // Це нормальне завершення роботи при вимкненні ПК. Просто виходимо з циклу.
                    break;
                }
                catch (Exception)
                {
                    // Ігноруємо інші помилки (наприклад, якщо дублікат від'єднався завчасно)
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
