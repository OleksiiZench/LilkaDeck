using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using LilkaDeckApp.Mvvm;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.Services;

namespace LilkaDeckApp.ViewModels;

/// <summary>
/// Puts a picture on the selected deck button, whether it came from the file picker, the gallery or a drop.
/// All three end in <see cref="ImportAsync"/>, so they behave the same: convert, store, show, save at once.
/// </summary>
public sealed class IconImportViewModel : ObservableObject
{
    private readonly IIconImporter _importer;
    private readonly IButtonStore _store;
    private readonly IFilePicker _picker;
    private readonly DeckViewModel _deck;
    private readonly ActivityViewModel _activity;
    private readonly Func<Task> _saveNow;
    
    public IconImportViewModel(
        IIconImporter importer,
        IButtonStore store,
        IFilePicker picker,
        GalleryFolder galleryFolder,
        DeckViewModel deck,
        ActivityViewModel activity,
        Func<Task> saveNow)
    {
        _importer = importer;
        _store = store;
        _picker = picker;
        _deck = deck;
        _activity = activity;
        _saveNow = saveNow;
        
        BrowseCommand = new AsyncRelayCommand(BrowseAsync, onError: ReportFailure);
        Gallery = new IconGalleryViewModel(galleryFolder, SelectFromGalleryAsync, ReportFailure, activity);
    }
    
    /// <summary>Raised after the icon is stored for the selected button and before it is saved to the device.</summary>
    public event Action<ImportedIcon>? Imported;
    
    public ICommand BrowseCommand { get; }
    
    public IconGalleryViewModel Gallery { get; }
    
    /// <summary>Imports a picture for the selected button; does nothing when no button is selected.</summary>
    public async Task ImportAsync(string sourcePath)
    {
        if (_deck.SelectedPosition is not { } position) return;
        
        var icon = TryImport(sourcePath);
        if (icon == null) return;
        
        _store.SetIcon(position, icon);
        Imported?.Invoke(icon);
        await _saveNow();
    }
    
    private ImportedIcon? TryImport(string sourcePath)
    {
        try
        {
            return _importer.Import(sourcePath);
        }
        catch (IconImportException ex)
        {
            _activity.Error(ex.Error == IconImportError.UnsupportedFormat
                ? "Помилка: підтримуються лише формати PNG, JPG, JPEG та RAW."
                : "Помилка: файл .raw має бути 64×64 пікселі у форматі RGB565 (8192 байти).");
            return null;
        }
        catch (Exception ex)
        {
            _activity.Error($"Помилка конвертації: {ex.Message}");
            return null;
        }
    }
    
    private async Task BrowseAsync()
    {
        if (_deck.SelectedPosition == null) return;
        
        string? path = await _picker.PickImageAsync();
        if (path != null) await ImportAsync(path);
    }
    
    private async Task SelectFromGalleryAsync(string sourcePath)
    {
        if (_deck.SelectedPosition == null) return;
        
        _activity.Info($"Обрано з галереї: {Path.GetFileName(sourcePath)}");
        Gallery.Close();
        await ImportAsync(sourcePath);
    }
    
    private void ReportFailure(Exception ex) => _activity.Error($"Помилка: {ex.Message}");
}
