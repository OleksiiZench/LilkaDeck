using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace LilkaDeckApp.Services;

public class LilkaCommunicationService : IDisposable
{
    private SerialPort? _serialPort;
    private TaskCompletionSource<string>? _ackTcs;
    private CancellationTokenSource? _scannerCts;
    private bool _isSyncingActive = false; // Блокує Heartbeat під час передачі файлів

    public event Action<string>? OnExecuteRequested;
    public event Action<string>? OnLogMessage;
    public event Action<Exception>? OnError;

    // Нові події для UI
    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;

    public bool IsConnected { get; private set; }
    public string ConnectedPortName => _serialPort?.PortName ?? string.Empty;

    public void StartAutoScanner()
    {
        _scannerCts?.Cancel();
        _scannerCts = new CancellationTokenSource();
        _ = ScanAndMonitorLoopAsync(_scannerCts.Token);
    }

    private async Task ScanAndMonitorLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!IsConnected)
            {
                // РЕЖИМ 1: ПОШУК ПРИСТРОЮ
                string[] ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    if (token.IsCancellationRequested) break;
                    if (await TryConnectAndPingAsync(port))
                    {
                        IsConnected = true;
                        OnConnected?.Invoke(port);
                        break; // Знайшли Лілку, зупиняємо пошук
                    }
                }
            }
            else if (!_isSyncingActive)
            {
                // РЕЖИМ 2: HEARTBEAT (Моніторинг з'єднання)
                try
                {
                    _serialPort!.WriteLine("PING");
                    if (!await WaitForAck("LILKA_PONG:v1.0", 1000))
                    {
                        throw new Exception("Heartbeat timeout"); // Немає пульсу
                    }
                }
                catch
                {
                    Disconnect(); // Від'єднуємо і скидаємо статус
                }
            }

            // Пауза 2 секунди між скануваннями / серцебиттями
            await Task.Delay(2000, token);
        }
    }

    private async Task<bool> TryConnectAndPingAsync(string portName)
    {
        try
        {
            _serialPort = new SerialPort(portName, 115200) { ReadTimeout = 100 };
            _serialPort.DtrEnable = true;
            _serialPort.RtsEnable = true;
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();

            // Даємо платі 1.5 секунди на перезавантаження (через DTR)
            await Task.Delay(1500);

            _serialPort.WriteLine("PING");

            // Якщо плата відповіла нашим PONG - це Лілка!
            if (await WaitForAck("LILKA_PONG:v1.0", 1000))
            {
                return true;
            }

            // Це якийсь інший пристрій (наприклад, 3D принтер)
            DisconnectInternal();
            return false;
        }
        catch
        {
            DisconnectInternal();
            return false;
        }
    }

    public void Disconnect()
    {
        if (!IsConnected) return;
        IsConnected = false;
        DisconnectInternal();
        OnDisconnected?.Invoke(); // Сповіщаємо UI
    }

    private void DisconnectInternal()
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
                else if (data.StartsWith("ACK_") || data.StartsWith("LILKA_PONG") || data.StartsWith("PROFILES:"))
                {
                    _ackTcs?.TrySetResult(data);
                }
                else if (data.Length > 0)
                {
                    OnLogMessage?.Invoke($"[ESP32] {data}");
                }
            }
        }
        catch (TimeoutException) { }
        catch (Exception ex) { OnError?.Invoke(ex); }
    }

    public async Task SyncDataAsync(int profileId, byte[] jsonBytes, Dictionary<string, string> filesToSend, IProgress<int> progress, IProgress<string> status)
    {
        if (!IsConnected) throw new InvalidOperationException("Not connected to Lilka.");

        _isSyncingActive = true; // Ставимо Heartbeat на паузу
        try
        {
            int totalFiles = 1 + filesToSend.Count;
            int currentFile = 0;

            status.Report($"Запуск (Профіль {profileId})...");
            _serialPort!.WriteLine($"SYNC_START:{profileId}");
            if (!await WaitForAck("ACK_SYNC", 3000)) throw new Exception("Лілка не відповіла на SYNC_START");

            status.Report("Відправка config.json...");
            await SendFileChunksAsync("config.json", jsonBytes);
            currentFile++;
            progress.Report((currentFile * 100) / totalFiles);

            foreach (var file in filesToSend)
            {
                status.Report($"Відправка {file.Key}...");
                byte[] imgBytes = await File.ReadAllBytesAsync(file.Value);
                await SendFileChunksAsync(file.Key, imgBytes);

                currentFile++;
                progress.Report((currentFile * 100) / totalFiles);
            }

            status.Report("Перезавантаження UI Лілки...");
            _serialPort.WriteLine("SYNC_END");
            if (!await WaitForAck("ACK_END", 3000)) throw new Exception("Лілка не відповіла на SYNC_END");
        }
        finally
        {
            _isSyncingActive = false; // Відновлюємо Heartbeat
        }
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
        _scannerCts?.Cancel();
        DisconnectInternal();
    }

    public async Task<string[]> GetProfilesListAsync()
    {
        if (!IsConnected) return Array.Empty<string>();

        _serialPort!.WriteLine("GET_PROFILES");

        var timeoutTask = Task.Delay(2000);
        _ackTcs = new TaskCompletionSource<string>();

        var completedTask = await Task.WhenAny(_ackTcs.Task, timeoutTask);
        if (completedTask == timeoutTask) return Array.Empty<string>();

        string response = await _ackTcs.Task;
        if (response.StartsWith("PROFILES:"))
        {
            string data = response.Substring(9).Trim();
            if (string.IsNullOrEmpty(data)) return Array.Empty<string>();

            return data.Split(',')
                       .OrderBy(id => int.Parse(id))
                       .ToArray();
        }
        return Array.Empty<string>();
    }
    
    public void SendColorPreview(string hexColor)
    {
        if (!IsConnected) return;
        try 
        {
            // Fire-and-forget відправка. Не чекаємо ACK, щоб не блокувати UI при швидкому перетягуванні палітри
            _serialPort!.WriteLine($"SET_COLOR:{hexColor}");
        } 
        catch { /* Ігноруємо помилки під час прев'ю */ }
    }

    public async Task<byte[]?> DownloadFileAsync(int profileId, string fileName)
    {
        if (!IsConnected) return null;

        // Тимчасово відключаємо текстовий парсер, щоб він не зламався від бінарних даних картинки
        _serialPort!.DataReceived -= SerialPort_DataReceived;
        _isSyncingActive = true;

        try
        {
            _serialPort.WriteLine($"FILE_GET:{profileId}:{fileName}");
            
            // Чекаємо підтвердження і розмір файлу
            string response = "";
            int retries = 20; // 2 секунди таймаут (20 * 100ms)
            while (retries-- > 0)
            {
                try 
                { 
                    response = _serialPort.ReadLine().Trim();
                    
                    // Якщо дочекалися потрібної відповіді - виходимо з циклу
                    if (response.StartsWith("FILE_SEND_START:") || response == "ERR:FILE_NOT_FOUND") break;
                    
                    // Якщо прилетів якийсь інший лог - просто виводимо його і слухаємо далі
                    if (response.Length > 0) OnLogMessage?.Invoke($"[ESP32] {response}");
                }
                catch (TimeoutException) { }
            }

            if (response == "ERR:FILE_NOT_FOUND" || string.IsNullOrEmpty(response)) return null;
            if (!response.StartsWith("FILE_SEND_START:")) throw new Exception("Unexpected response");

            int size = int.Parse(response.Substring(16));
            byte[] buffer = new byte[size];
            int totalRead = 0;
            
            // Читаємо сирі байти
            while(totalRead < size)
            {
                int read = _serialPort.BaseStream.Read(buffer, totalRead, size - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
            return buffer;
        }
        finally
        {
            _isSyncingActive = false;
            // Повертаємо текстовий парсер на місце
            _serialPort.DataReceived += SerialPort_DataReceived;
        }
    }
}
