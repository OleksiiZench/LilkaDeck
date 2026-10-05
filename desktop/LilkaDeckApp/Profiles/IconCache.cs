using System.IO;

namespace LilkaDeckApp.Profiles;

/// <summary>
/// Keeps copies of icon files in one directory, so they survive when the original is moved or deleted.
/// </summary>
public sealed class IconCache
{
    public IconCache(string directoryPath)
    {
        DirectoryPath = directoryPath;
        Directory.CreateDirectory(directoryPath);
    }

    public string DirectoryPath { get; }

    /// <summary>Returns the cached file for the icon, or null when it is not cached.</summary>
    public string? Find(string iconFileName)
    {
        if (string.IsNullOrWhiteSpace(iconFileName)) return null;

        string path = Path.Combine(DirectoryPath, iconFileName);
        return File.Exists(path) ? path : null;
    }

    public string Store(string sourcePath, string iconFileName)
    {
        string cachedPath = Path.Combine(DirectoryPath, iconFileName);
        if (sourcePath != cachedPath)
        {
            File.Copy(sourcePath, cachedPath, true);
        }
        return cachedPath;
    }

    public string Save(string iconFileName, byte[] content)
    {
        string cachedPath = Path.Combine(DirectoryPath, iconFileName);
        File.WriteAllBytes(cachedPath, content);
        return cachedPath;
    }
}
