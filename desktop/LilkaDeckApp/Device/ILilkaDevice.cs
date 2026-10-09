using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LilkaDeckApp.Device;

/// <summary>The calls a connected Lilka understands. <see cref="LilkaDeviceClient"/> is the real one.</summary>
public interface ILilkaDevice
{
    Task<IReadOnlyList<int>> GetProfileIdsAsync(CancellationToken cancellationToken);
    
    Task<int> CreateProfileAsync(CancellationToken cancellationToken);
    
    Task<bool> DeleteProfileAsync(int profileId, CancellationToken cancellationToken);
    
    Task<byte[]?> DownloadFileAsync(int profileId, string fileName, CancellationToken cancellationToken);
    
    Task<bool> TrySetColorAsync(string hex, CancellationToken cancellationToken);
    
    Task SyncAsync(
        int profileId, SyncFile config, IReadOnlyList<SyncFile> icons, IProgress<SyncProgress> progress, CancellationToken cancellationToken);
}
