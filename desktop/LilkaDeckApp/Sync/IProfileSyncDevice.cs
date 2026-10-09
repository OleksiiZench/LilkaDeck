using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LilkaDeckApp.Device;

namespace LilkaDeckApp.Sync;

/// <summary>What saving a profile needs from the device.</summary>
public interface IProfileSyncDevice
{
    bool IsConnected { get; }
    
    Task SyncAsync(int profileId, SyncFile config, IReadOnlyList<SyncFile> icons, IProgress<SyncProgress> progress);
}
