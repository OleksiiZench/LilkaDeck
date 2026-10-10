using System;

namespace LilkaDeckApp.Device;

public enum DeviceStep
{
    StartSync, AcceptFile, WriteChunk, FinishFile, EndSync,
    ListProfiles, CreateProfile, DeleteProfile, DownloadFile
}

/// <summary>The device did not answer in time. Says which step was waiting, so the UI can word the message.</summary>
public sealed class DeviceTimeoutException : TimeoutException
{
    public DeviceTimeoutException(DeviceStep step, string? fileName = null, int offset = 0)
        : base($"The device did not answer during {step}.")
    {
        Step = step;
        FileName = fileName;
        Offset = offset;
    }

    public DeviceStep Step { get; }
    public string? FileName { get; }
    public int Offset { get; }
}

/// <summary>A save was requested while no Lilka is connected.</summary>
public sealed class DeviceNotConnectedException : InvalidOperationException
{
    public DeviceNotConnectedException()
        : base("No Lilka is connected.")
    {
    }
}
