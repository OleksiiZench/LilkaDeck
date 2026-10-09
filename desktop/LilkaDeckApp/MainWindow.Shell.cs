using System;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using LilkaDeckApp.Converters;
using LilkaDeckApp.Device;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.Services;
using LilkaDeckApp.Sync;
using LilkaDeckApp.ViewModels;

namespace LilkaDeckApp;

// The window's link to its view models: the log, the connection status and the profile screen.
public partial class MainWindow
{
    private MainViewModel _viewModel = null!;
    
    // Call once from the constructor, after _device and _session have been created.
    private void InitializeShell(ProfileCaches caches)
    {
        var gateway = new DeviceGateway(_device);
        var activity = new ActivityViewModel();
        var connection = new ConnectionViewModel();
        var profile = new ProfileViewModel(gateway, new ProfileLoader(gateway, caches), activity, connection, HandleError);
        var saver = new ProfileSaver(profile, _session, gateway, activity, HandleError);
        var deck = new DeckViewModel();
        var editor = new ButtonEditorViewModel(_session, () => profile.IsLoading);
        var icons = new IconImportViewModel(
            _session, _session, new AvaloniaFilePicker(this),
            new GalleryFolder(Path.Combine(AppContext.BaseDirectory, "assets", "standard_icons")),
            deck, activity, saver.SaveNowAsync);
        _viewModel = new MainViewModel(activity, connection, profile, deck, editor, icons);
        
        deck.Selected += editor.Show;
        editor.Edited += saver.RequestSave;
        
        icons.Imported += icon =>
        {
            editor.ShowImportedIcon(icon.FileName);
            RefreshDeck();
        };
        icons.Gallery.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IconGalleryViewModel.Items))
            {
                ThumbnailBitmapConverter.Instance.Retain(icons.Gallery.Items.Select(item => item.FilePath));
            }
        };
        
        profile.NameEdited += saver.RequestSave;
        profile.ColorEdited += hex =>
        {
            gateway.PreviewColor(hex);
            saver.RequestSave();
        };
        profile.Loaded += OnProfileLoaded;
        profile.Removed += () => _session.Clear();
        
        DataContext = _viewModel;
        
        // Keeping the newest line in view is a matter for the view, not for the view model.
        activity.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActivityViewModel.LogText))
            {
                Dispatcher.UIThread.Post(() => LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0);
            }
        };
    }
    
    private void OnProfileLoaded(int profileId, ProfileDocument? document)
    {
        if (document != null)
        {
            _session.ShowProfile(profileId, document);
            RefreshDeck();
        }
        
        if (_viewModel.Deck.SelectedPosition is { } selected) _viewModel.Editor.Show(selected);
    }
    
    private void RefreshDeck()
    {
        _viewModel.Deck.Refresh(_session.GetIconFilePath);
        IconBitmapConverter.Instance.Retain(_viewModel.Deck.IconFilePaths);
    }
    
    // Safe to call from any thread: the log is only touched on the UI thread.
    private void AppLog(string message, bool isError = false) =>
    Dispatcher.UIThread.Post(() =>
    {
        if (isError) _viewModel.Activity.Error(message);
        else _viewModel.Activity.Info(message);
    });
    
    private async void HandleConnected(string portName)
    {
        Dispatcher.UIThread.Post(() => _viewModel.Connection.SetConnected(portName));
        AppLog($"Підключено до порту {portName}");
        AppLog("Завантаження конфігурації з Лілки...");
        
        await Dispatcher.UIThread.InvokeAsync(() => _viewModel.Profile.RefreshAsync());
    }
    
    private void HandleDisconnected()
    {
        Dispatcher.UIThread.Post(() => _viewModel.Connection.SetSearching());
        AppLog("Пристрій відключено. Пошук...", true);
    }
    
    private void HandleLogMessage(string message) => AppLog($"[ESP32] {message}");
    
    private void HandleError(Exception ex)
    {
        string text = DeviceErrorText.Describe(ex);
        Dispatcher.UIThread.Post(() => _viewModel.Connection.SetError(text));
        AppLog($"Синхронізацію перервано: {text}", true);
    }
}
