using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LilkaDeckApp.Mvvm;
using LilkaDeckApp.Profiles;

namespace LilkaDeckApp.ViewModels;

/// <summary>The pop-up with the ready-made icons. It reads the folder each time it opens, so new files appear at once.</summary>
public sealed class IconGalleryViewModel : ObservableObject
{
    private readonly GalleryFolder _folder;
    private readonly Func<string, Task> _onSelected;
    private readonly Action<Exception> _onError;
    private readonly ActivityViewModel _activity;
    private IReadOnlyList<GalleryIconViewModel> _items = Array.Empty<GalleryIconViewModel>();
    private bool _isOpen;
    
    public IconGalleryViewModel(
        GalleryFolder folder, Func<string, Task> onSelected, Action<Exception> onError, ActivityViewModel activity)
    {
        _folder = folder;
        _onSelected = onSelected;
        _onError = onError;
        _activity = activity;
        OpenCommand = new RelayCommand(Open);
    }
    
    public ICommand OpenCommand { get; }
    
    public IReadOnlyList<GalleryIconViewModel> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }
    
    public bool IsOpen
    {
        get => _isOpen;
        set => SetProperty(ref _isOpen, value);
    }
    
    public void Close() => IsOpen = false;
    
    private void Open()
    {
        try
        {
            var listing = _folder.List();
            if (listing.FolderWasCreated)
            {
                _activity.Info("Створено папку для галереї: " + _folder.DirectoryPath);
                return;
            }
            if (listing.Files.Count == 0)
            {
                _activity.Info("Галерея порожня. Додайте картинки в папку " + _folder.DirectoryPath);
                return;
            }
            
            Items = listing.Files.Select(file => new GalleryIconViewModel(file, _onSelected, _onError)).ToList();
            IsOpen = true;
        }
        catch (Exception ex)
        {
            _activity.Error($"Помилка завантаження галереї: {ex.Message}");
        }
    }
}
