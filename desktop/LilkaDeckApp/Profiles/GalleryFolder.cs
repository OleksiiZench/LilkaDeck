using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LilkaDeckApp.Profiles;

public sealed record GalleryListing(IReadOnlyList<string> Files, bool FolderWasCreated);

/// <summary>The folder with the ready-made icons that ship with the application.</summary>
public sealed class GalleryFolder
{
    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };
    
    public GalleryFolder(string directoryPath)
    {
        DirectoryPath = directoryPath;
    }
    
    public string DirectoryPath { get; }
    
    /// <summary>Lists the pictures by name. A missing folder is created, so the user sees where to put icons.</summary>
    public GalleryListing List()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Directory.CreateDirectory(DirectoryPath);
            return new GalleryListing(Array.Empty<string>(), FolderWasCreated: true);
        }
        
        var files = Directory.GetFiles(DirectoryPath)
            .Where(file => Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .OrderBy(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new GalleryListing(files, FolderWasCreated: false);
    }
}
