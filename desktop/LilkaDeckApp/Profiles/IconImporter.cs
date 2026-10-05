using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LilkaDeckApp.Imaging;

namespace LilkaDeckApp.Profiles;

public sealed record ImportedIcon(string FileName, string CachedPath);

public enum IconImportError { UnsupportedFormat, InvalidRawFile }

public sealed class IconImportException : Exception
{
    public IconImportException(IconImportError error, string message) : base(message)
    {
        Error = error;
    }

    public IconImportError Error { get; }
}

/// <summary>
/// Turns a picture the user chose into an icon file in the cache, ready to be sent to the device.
/// Nothing is written next to the original file.
/// </summary>
public sealed class IconImporter
{
    private const string RawExtension = ".raw";

    public static IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".png", ".jpg", ".jpeg", RawExtension };

    private readonly IconCache _cache;

    public IconImporter(IconCache cache)
    {
        _cache = cache;
    }

    public ImportedIcon Import(string sourcePath)
    {
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            throw new IconImportException(IconImportError.UnsupportedFormat, $"Unsupported icon format '{extension}'.");
        }

        string fileName = IconFileName.FromSource(Path.GetFileNameWithoutExtension(sourcePath));
        string cachedPath = extension == RawExtension
            ? StoreRaw(sourcePath, fileName)
            : _cache.Save(fileName, RawIconEncoder.Encode(sourcePath));

        return new ImportedIcon(fileName, cachedPath);
    }

    // A raw file is used as it is, but only if it really has the layout the display expects.
    private string StoreRaw(string sourcePath, string fileName)
    {
        long size = new FileInfo(sourcePath).Length;
        if (size != RawIconFormat.FileSizeBytes)
        {
            throw new IconImportException(
                IconImportError.InvalidRawFile, $"A raw icon must be {RawIconFormat.FileSizeBytes} bytes, got {size}.");
        }
        return _cache.Store(sourcePath, fileName);
    }
}
