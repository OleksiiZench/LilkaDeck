using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using LilkaDeckApp.Device;
using LilkaDeckApp.Transport;

namespace LilkaDeckApp.Services;

/// <summary>
/// Compatibility facade that keeps the API and the messages MainWindow was written against.
/// It can be deleted once the UI works with LilkaDeviceClient and words its own messages.
/// </summary>
public sealed class LilkaCommunicationService : IDisposable
{
    private readonly ConnectionMonitor _monitor;
    private readonly DeviceTimeouts? _timeouts;
    private volatile LilkaDeviceClient? _client;

    public LilkaCommunicationService()
        : this(new ConnectionMonitor(SerialPort.GetPortNames, port => new SerialPortTransport(port)))
    {
    }

    public LilkaCommunicationService(ConnectionMonitor monitor, DeviceTimeouts? timeouts = null)
    {
        _monitor = monitor;
        _timeouts = timeouts;

        _monitor.SessionCreated += AttachToSession;
        _monitor.Connected += HandleConnected;
        _monitor.Disconnected += HandleDisconnected;
    }

    public event Action<string>? OnExecuteRequested;
    public event Action<string>? OnLogMessage;
    public event Action<Exception>? OnError;
    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;

    public bool IsConnected => _monitor.IsConnected;
    public string ConnectedPortName => _monitor.ConnectedPortName;

    public void StartAutoScanner() => _monitor.Start();

    public void Disconnect() => _monitor.Disconnect();

    public void Dispose() => _monitor.Dispose();

    public async Task SyncDataAsync(
        int profileId, byte[] jsonBytes, Dictionary<string, string> filesToSend, IProgress<int> progress, IProgress<string> status)
    {
        var client = _client;
        if (client == null || !IsConnected) throw new InvalidOperationException("Not connected to Lilka.");

        // Reading everything first means a missing file stops the sync before anything is sent.
        var icons = new List<SyncFile>();
        foreach (var (name, path) in filesToSend)
        {
            icons.Add(new SyncFile(name, await File.ReadAllBytesAsync(path)));
        }

        try
        {
            await client.SyncAsync(profileId, new SyncFile("config.json", jsonBytes), icons,
                new LegacyProgress(profileId, progress, status), default);
        }
        catch (DeviceTimeoutException ex)
        {
            throw new Exception(DescribeTimeout(ex), ex);
        }
    }

    public async Task<string[]> GetProfilesListAsync()
    {
        var ids = await QueryAsync<IReadOnlyList<int>>(c => c.GetProfileIdsAsync(default), Array.Empty<int>());
        return ids.OrderBy(id => id).Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray();
    }

    public Task<int?> CreateProfileAsync() =>
        QueryAsync<int?>(async c => await c.CreateProfileAsync(default), null);

    public Task<bool> DeleteProfileAsync(int profileId) =>
        QueryAsync(c => c.DeleteProfileAsync(profileId, default), false);

    // A silent device looks like a missing file, as it always did.
    public Task<byte[]?> DownloadFileAsync(int profileId, string fileName) =>
        QueryAsync<byte[]?>(c => c.DownloadFileAsync(profileId, fileName, default), null);

    public void SendColorPreview(string hexColor)
    {
        var client = _client;
        if (client != null && IsConnected) _ = IgnoreFailuresAsync(client.TrySetColorAsync(hexColor, default));
    }

    private async Task<T> QueryAsync<T>(Func<LilkaDeviceClient, Task<T>> query, T fallback)
    {
        var client = _client;
        if (client == null || !IsConnected) return fallback;

        try
        {
            return await query(client);
        }
        catch (Exception ex) when (ex is DeviceTimeoutException or TransportClosedException)
        {
            return fallback;
        }
    }

    private static async Task IgnoreFailuresAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
            // A lost color preview is harmless.
        }
    }

    private void AttachToSession(DeviceSession session)
    {
        session.LogReceived += text => OnLogMessage?.Invoke($"[ESP32] {text}");
        session.ExecuteRequested += target => OnExecuteRequested?.Invoke(target);
    }

    private void HandleConnected(string portName)
    {
        _client = new LilkaDeviceClient(_monitor.Session!, _timeouts);
        OnConnected?.Invoke(portName);
    }

    private void HandleDisconnected(Exception? failure)
    {
        _client = null;
        OnDisconnected?.Invoke();
        if (failure != null && failure is not TransportClosedException) OnError?.Invoke(failure);
    }

    private static string DescribeTimeout(DeviceTimeoutException ex) => ex.Step switch
    {
        DeviceStep.StartSync => "Лілка не відповіла на SYNC_START",
        DeviceStep.AcceptFile => $"Немає ACK_FILE для {ex.FileName}",
        DeviceStep.WriteChunk => $"Лілка зависла на записі {ex.FileName} (offset: {ex.Offset})",
        DeviceStep.FinishFile => $"Немає ACK_DONE для {ex.FileName}",
        DeviceStep.EndSync => "Лілка не відповіла на SYNC_END",
        _ => ex.Message
    };

    // Reports straight away, in order, like the old code did, instead of posting to a synchronization context.
    private sealed class LegacyProgress : IProgress<SyncProgress>
    {
        private readonly int _profileId;
        private readonly IProgress<int> _percent;
        private readonly IProgress<string> _status;

        public LegacyProgress(int profileId, IProgress<int> percent, IProgress<string> status)
        {
            _profileId = profileId;
            _percent = percent;
            _status = status;
        }

        public void Report(SyncProgress value)
        {
            switch (value.Stage)
            {
                case SyncStage.Started: _status.Report($"Запуск (Профіль {_profileId})..."); break;
                case SyncStage.FileStarted: _status.Report($"Відправка {value.FileName}..."); break;
                case SyncStage.FileFinished: _percent.Report(value.Percent); break;
                case SyncStage.Finishing: _status.Report("Перезавантаження UI Лілки..."); break;
            }
        }
    }
}
