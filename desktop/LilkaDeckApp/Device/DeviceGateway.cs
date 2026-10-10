using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.Sync;
using LilkaDeckApp.Transport;

namespace LilkaDeckApp.Device;

/// <summary>
/// What the profile screen, the loader and the saver need from the device. A device that is missing,
/// silent or gone gives a plain "nothing" (no ids, no file, false) instead of an exception,
/// except for saving, where a failure has to be reported.
/// </summary>
public sealed class DeviceGateway : IProfileDevice, IProfileFileSource, IProfileSyncDevice
{
    private readonly IDeviceSource _source;
    
    public DeviceGateway(IDeviceSource source)
    {
        _source = source;
    }
    
    public bool IsConnected => _source.Current != null;
    
    public async Task<IReadOnlyList<int>> GetProfileIdsAsync()
    {
        var ids = await QueryAsync<IReadOnlyList<int>>(device => device.GetProfileIdsAsync(default), Array.Empty<int>());
        return ids.Where(id => id >= 0).OrderBy(id => id).ToList();
    }
    
    public Task<int?> CreateProfileAsync() =>
    QueryAsync<int?>(async device => await device.CreateProfileAsync(default), null);
    
    public Task<bool> DeleteProfileAsync(int profileId) =>
    QueryAsync(device => device.DeleteProfileAsync(profileId, default), false);
    
    // A silent device looks like a missing file.
    public Task<byte[]?> DownloadFileAsync(int profileId, string fileName, CancellationToken cancellationToken) =>
    QueryAsync<byte[]?>(device => device.DownloadFileAsync(profileId, fileName, cancellationToken), null);
    
    public Task SyncAsync(int profileId, SyncFile config, IReadOnlyList<SyncFile> icons, IProgress<SyncProgress> progress)
    {
        var device = _source.Current ?? throw new DeviceNotConnectedException();
        return device.SyncAsync(profileId, config, icons, progress, default);
    }
    
    /// <summary>Shows a color on the device for a moment. Nothing happens if the device is missing or busy.</summary>
    public void PreviewColor(string hexColor)
    {
        var device = _source.Current;
        if (device != null) _ = IgnoreFailuresAsync(device.TrySetColorAsync(hexColor, default));
    }
    
    private async Task<T> QueryAsync<T>(Func<ILilkaDevice, Task<T>> query, T fallback)
    {
        var device = _source.Current;
        if (device == null) return fallback;
        
        try
        {
            return await query(device);
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
}
