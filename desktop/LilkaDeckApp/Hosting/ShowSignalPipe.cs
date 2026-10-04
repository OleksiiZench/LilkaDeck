using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace LilkaDeckApp.Hosting;

/// <summary>
/// Lets a second launch ask the running instance to show its window. The pipe is named per user,
/// so two users on the same machine do not talk to each other's application.
/// </summary>
public sealed class ShowSignalPipe : IDisposable
{
    private const string ShowCommand = "SHOW";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly CancellationTokenSource _stop = new();

    private ShowSignalPipe()
    {
    }

    private static string PipeName => $"LilkaDeck_IPC_{Environment.UserName}";

    /// <summary>Asks the running instance to show itself. Does nothing if none answers.</summary>
    public static void NotifyRunningInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect((int)ConnectTimeout.TotalMilliseconds);
            using var writer = new StreamWriter(client);
            writer.WriteLine(ShowCommand);
        }
        catch (Exception)
        {
            // Nobody is listening, or it did not answer in time.
        }
    }

    public static ShowSignalPipe Listen(Action onShowRequested)
    {
        var pipe = new ShowSignalPipe();
        _ = Task.Run(() => pipe.ServeAsync(onShowRequested));
        return pipe;
    }

    public void Dispose() => _stop.Cancel();

    private async Task ServeAsync(Action onShowRequested)
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                if (await ReceiveCommandAsync() == ShowCommand) onShowRequested();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Waiting before the next attempt keeps a persistent failure from spinning the CPU.
                Trace.TraceWarning($"The show-window pipe failed: {ex.Message}");
                if (!await WaitBeforeRetryAsync()) return;
            }
        }
    }

    private async Task<string?> ReceiveCommandAsync()
    {
        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await server.WaitForConnectionAsync(_stop.Token);
        using var reader = new StreamReader(server);
        return await reader.ReadLineAsync(_stop.Token);
    }

    private async Task<bool> WaitBeforeRetryAsync()
    {
        try
        {
            await Task.Delay(RetryDelay, _stop.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
