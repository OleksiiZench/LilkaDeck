using System;
using System.Diagnostics;

namespace LilkaDeckApp.Services;

public interface ITargetLauncher
{
    void Launch(string target);
}

/// <summary>Opens a program, file or URL the way the operating system would if the user had opened it.</summary>
public sealed class ShellTargetLauncher : ITargetLauncher
{
    public void Launch(string target)
    {
        using var process = Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
    }
}

/// <summary>Carries out the device's requests to launch something on this computer.</summary>
public sealed class LaunchHandler
{
    private readonly ITargetLauncher _launcher;

    public LaunchHandler(ITargetLauncher launcher)
    {
        _launcher = launcher;
    }

    public bool TryLaunch(string target, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(target))
        {
            error = "The launch target is empty.";
            return false;
        }

        try
        {
            _launcher.Launch(target);
            return true;
        }
        catch (Exception ex)
        {
            // Whatever goes wrong while starting a program is reported, never allowed to stop the application.
            error = ex.Message;
            return false;
        }
    }
}
