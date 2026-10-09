using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LilkaDeckApp.Profiles;

namespace LilkaDeckApp.Services;

/// <summary>The file picker of the operating system, opened over a window.</summary>
public sealed class AvaloniaFilePicker : IFilePicker
{
    private readonly TopLevel _owner;
    
    public AvaloniaFilePicker(TopLevel owner)
    {
        _owner = owner;
    }
    
    public async Task<string?> PickImageAsync()
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Оберіть картинку",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = IconImporter.SupportedExtensions.Select(extension => "*" + extension).ToArray()
                }
            }
        });
        
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
