using System;
using System.Diagnostics;
using System.Threading;

namespace LilkaDeckApp.Hosting;

/// <summary>
/// Guarantees the process really ends. If a normal shutdown has not finished after the grace period,
/// it leaves the process through the runtime; if even that hangs, it kills the process.
/// Background threads, so a shutdown that works is never delayed by it.
/// </summary>
public static class ExitWatchdog
{
    private static readonly TimeSpan KillDelayAfterExit = TimeSpan.FromSeconds(2);
    private static int _armed;

    public static void Arm(TimeSpan gracePeriod)
    {
        if (Interlocked.Exchange(ref _armed, 1) != 0) return;

        RunAfter(gracePeriod, () => Environment.Exit(0));
        RunAfter(gracePeriod + KillDelayAfterExit, () => Process.GetCurrentProcess().Kill());
    }

    private static void RunAfter(TimeSpan delay, Action action)
    {
        new Thread(() =>
        {
            Thread.Sleep(delay);
            action();
        })
        { IsBackground = true }.Start();
    }
}
