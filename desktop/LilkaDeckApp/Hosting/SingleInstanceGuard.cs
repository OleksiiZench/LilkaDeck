using System;
using System.IO;

namespace LilkaDeckApp.Hosting;

/// <summary>
/// Makes sure only one copy of the application runs per user, by holding an exclusive lock on a file.
/// The lock disappears with the process, so a crash cannot leave a stale one behind.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly FileStream _lockFile;

    private SingleInstanceGuard(FileStream lockFile)
    {
        _lockFile = lockFile;
    }

    /// <summary>Returns null when another instance already holds the lock.</summary>
    public static SingleInstanceGuard? TryAcquire()
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LilkaDeck");
        Directory.CreateDirectory(directory);

        try
        {
            var lockFile = new FileStream(
                Path.Combine(directory, "single_instance.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return new SingleInstanceGuard(lockFile);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Dispose() => _lockFile.Dispose();
}
