using System;
using LilkaDeckApp.Device;

namespace LilkaDeckApp.ViewModels;

/// <summary>Words a failure for the user.</summary>
public static class DeviceErrorText
{
    public static string Describe(Exception exception) =>
    exception is DeviceTimeoutException timeout ? Describe(timeout) : exception.Message;
    
    private static string Describe(DeviceTimeoutException timeout) => timeout.Step switch
    {
        DeviceStep.StartSync => "Лілка не відповіла на SYNC_START",
        DeviceStep.AcceptFile => $"Немає ACK_FILE для {timeout.FileName}",
        DeviceStep.WriteChunk => $"Лілка зависла на записі {timeout.FileName} (offset: {timeout.Offset})",
        DeviceStep.FinishFile => $"Немає ACK_DONE для {timeout.FileName}",
        DeviceStep.EndSync => "Лілка не відповіла на SYNC_END",
        _ => timeout.Message
    };
}
