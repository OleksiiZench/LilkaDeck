using System;
using System.Diagnostics;

namespace LilkaDeckApp.Device;

/// <summary>Raises an event so that a faulty listener cannot take the caller down with it.</summary>
internal static class SafeEvent
{
    public static void Raise<T>(Action<T>? handler, T argument, string source)
    {
        try
        {
            handler?.Invoke(argument);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"A {source} event handler failed: {ex.Message}");
        }
    }
}
