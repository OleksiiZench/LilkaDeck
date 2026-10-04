using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LilkaDeckApp.Hosting;

/// <summary>
/// Turns Ctrl+C and the system's termination request (logout, shutdown) into a call to the given action,
/// instead of letting the runtime kill the process.
/// </summary>
public sealed class ShutdownSignals : IDisposable
{
    // The registrations are kept in a field on purpose: an unreferenced one can be finalized,
    // which quietly unregisters the handler.
    private readonly List<PosixSignalRegistration> _registrations = new();

    public ShutdownSignals(Action onSignal)
    {
        foreach (var signal in new[] { PosixSignal.SIGINT, PosixSignal.SIGTERM })
        {
            try
            {
                _registrations.Add(PosixSignalRegistration.Create(signal, context =>
                {
                    context.Cancel = true;
                    onSignal();
                }));
            }
            catch (PlatformNotSupportedException)
            {
                // This signal does not exist on the current platform.
            }
        }
    }

    public void Dispose()
    {
        foreach (var registration in _registrations) registration.Dispose();
    }
}
