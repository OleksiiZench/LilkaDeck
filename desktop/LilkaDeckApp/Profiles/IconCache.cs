using System;
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

    /// <summary>A plain file name, never a path: it must not lead out of the cache folder.</summary>
    public static bool IsSafeFileName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name == Path.GetFileName(name) && name != "." && name != "..";

    /// <summary>Returns the cached file for the icon, or null when it is not cached.</summary>
    public string? Find(string iconFileName)
    {
        if (!IsSafeFileName(iconFileName)) return null;

        string path = Path.Combine(DirectoryPath, iconFileName);
        return File.Exists(path) ? path : null;
    }

    public string Store(string sourcePath, string iconFileName)
    {
        string cachedPath = PathFor(iconFileName);
        if (sourcePath != cachedPath)
        {
            File.Copy(sourcePath, cachedPath, true);
        }
        return cachedPath;
    }

    public string Save(string iconFileName, byte[] content)
    {
        string cachedPath = PathFor(iconFileName);
        File.WriteAllBytes(cachedPath, content);
        return cachedPath;
    }

    private string PathFor(string iconFileName)
    {
        if (!IsSafeFileName(iconFileName))
        {
            throw new ArgumentException($"'{iconFileName}' is not a plain file name.", nameof(iconFileName));
        }
        return Path.Combine(DirectoryPath, iconFileName);
    }
}
