using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace LilkaDeckApp.Services;

public class LilkaCommunicationService : IDisposable
{
    private SerialPort? _serialPort;
    private TaskCompletionSource<string>? _ackTcs;

    // Events to notify the UI or system about background actions
    public event Action<string>? OnExecuteRequested;
    public event Action<string>? OnLogMessage;
    public event Action<Exception>? OnError;

    public bool IsConnected => _serialPort != null && _serialPort.IsOpen;
    public string ConnectedPortName => _serialPort?.PortName ?? string.Empty;

    /// <summary>
    /// Attempts to open a Native USB CDC connection to the ESP32.
    /// </summary>
    public void Connect(string portName)
    {
        Disconnect();

        _serialPort = new SerialPort(portName, 115200) { ReadTimeout = 100 };
        _serialPort.DtrEnable = true;
        _serialPort.RtsEnable = true;
        _serialPort.DataReceived += SerialPort_DataReceived;
        _serialPort.Open();
    }

    /// <summary>
    /// Safely closes the connection.
    /// </summary>
    public void Disconnect()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.DataReceived -= SerialPort_DataReceived;
            _serialPort.Close();
            _serialPort.Dispose();
        }
        _serialPort = null;
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            while (_serialPort.BytesToRead > 0)
            {
                string data = _serialPort.ReadLine().Trim();

                if (data.StartsWith("EXECUTE:"))
                {
                    OnExecuteRequested?.Invoke(data.Substring(8).Trim());
                }
                else if (data.StartsWith("ACK_"))
                {
                    _ackTcs?.TrySetResult(data);
                }
                else if (data.Length > 0)
                {
                    OnLogMessage?.Invoke($"[ESP32] {data}");
                }
            }
        }
        catch (TimeoutException) { /* Normal during read loops */ }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Executes the full Ping-Pong synchronization protocol.
    /// Uses IProgress to safely update the UI thread.
    /// </summary>
    public async Task SyncDataAsync(int profileId, byte[] jsonBytes, Dictionary<string, string> filesToSend, IProgress<int> progress, IProgress<string> status)
    {
        if (!IsConnected) throw new InvalidOperationException("Not connected to Lilka.");

        int totalFiles = 1 + filesToSend.Count;
        int currentFile = 0;

        // 1. Start Sync
        status.Report($"Запуск (Профіль {profileId})...");
        _serialPort!.WriteLine($"SYNC_START:{profileId}");
        if (!await WaitForAck("ACK_SYNC", 3000)) throw new Exception("Лілка не відповіла на SYNC_START");

        // 2. Send JSON
        status.Report("Відправка config.json...");
        await SendFileChunksAsync("config.json", jsonBytes);
        currentFile++;
        progress.Report((currentFile * 100) / totalFiles);

        // 3. Send Images
        foreach (var file in filesToSend)
        {
            status.Report($"Відправка {file.Key}...");
            byte[] imgBytes = await File.ReadAllBytesAsync(file.Value);
            await SendFileChunksAsync(file.Key, imgBytes);

            currentFile++;
            progress.Report((currentFile * 100) / totalFiles);
        }

        // 4. End Sync
        status.Report("Перезавантаження UI Лілки...");
        _serialPort.WriteLine("SYNC_END");
        if (!await WaitForAck("ACK_END", 3000)) throw new Exception("Лілка не відповіла на SYNC_END");
    }

    private async Task SendFileChunksAsync(string fileName, byte[] data)
    {
        _serialPort!.WriteLine($"FILE_START:{fileName}:{data.Length}");
        if (!await WaitForAck("ACK_FILE", 3000)) throw new Exception($"Немає ACK_FILE для {fileName}");

        int offset = 0;
        int chunkSize = 256;

        while (offset < data.Length)
        {
            int size = Math.Min(chunkSize, data.Length - offset);
            _serialPort.Write(data, offset, size);
            offset += size;

            if (offset < data.Length)
            {
                if (!await WaitForAck("ACK_CHUNK", 5000))
                    throw new Exception($"Лілка зависла на записі {fileName} (offset: {offset})");
            }
        }

        if (!await WaitForAck("ACK_DONE", 8000)) throw new Exception($"Немає ACK_DONE для {fileName}");
    }

    private async Task<bool> WaitForAck(string expectedAck, int timeoutMs)
    {
        _ackTcs = new TaskCompletionSource<string>();
        var timeoutTask = Task.Delay(timeoutMs);

        var completedTask = await Task.WhenAny(_ackTcs.Task, timeoutTask);
        if (completedTask == timeoutTask) return false;

        return await _ackTcs.Task == expectedAck;
    }

    public void Dispose()
    {
        Disconnect();
    }
}
