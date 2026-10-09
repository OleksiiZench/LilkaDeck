namespace LilkaDeckApp.Device;

/// <summary>Tells which Lilka is connected right now.</summary>
public interface IDeviceSource
{
    /// <summary>The connected device, or null when there is none.</summary>
    ILilkaDevice? Current { get; }
}
