using System;
using System.IO;
using System.IO.Ports;
using LilkaDeckApp.Device;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.Services;
using LilkaDeckApp.Sync;
using LilkaDeckApp.Transport;
using LilkaDeckApp.ViewModels;

namespace LilkaDeckApp.Composition;

/// <summary>
/// The running application without a window: the one place where the parts are created and connected.
/// The window only shows <see cref="ViewModel"/>.
/// </summary>
public sealed class DeckApplication : IDisposable
{
    private readonly DeviceConnection _device;
    
    private DeckApplication(MainViewModel viewModel, DeviceConnection device)
    {
        ViewModel = viewModel;
        _device = device;
    }
    
    public MainViewModel ViewModel { get; }
    
    /// <summary>Starts looking for the Lilka.</summary>
    public void Start() => _device.Start();
    
    public void Dispose() => _device.Dispose();
    
    public static DeckApplication Create(IFilePicker filePicker, IUiDispatcher ui)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var caches = new ProfileCaches(Path.Combine(appData, "LilkaDeck", "Cache"));
        var galleryFolder = new GalleryFolder(Path.Combine(AppContext.BaseDirectory, "assets", "standard_icons"));
        
        var session = new DeckEditSession(caches);
        var device = new DeviceConnection(new ConnectionMonitor(SerialPort.GetPortNames, port => new SerialPortTransport(port)));
        var gateway = new DeviceGateway(device);
        
        var activity = new ActivityViewModel();
        var connection = new ConnectionViewModel();
        var failures = new FailureReporter(ui, activity, connection);
        var profile = new ProfileViewModel(gateway, new ProfileLoader(gateway, caches), activity, connection, failures.Report);
        var saver = new ProfileSaver(profile, session, gateway, activity, failures.Report);
        var deck = new DeckViewModel();
        var editor = new ButtonEditorViewModel(session, () => profile.IsLoading);
        var icons = new IconImportViewModel(session, session, filePicker, galleryFolder, deck, activity, saver.SaveNowAsync);
        
        _ = new DeviceEventPresenter(
            device, ui, activity, connection, failures, new LaunchHandler(new ShellTargetLauncher()), () => profile.RefreshAsync());
        
        // What changes on the deck, in the editor and in the profile screen reaches the others from here.
        deck.Selected += editor.Show;
        editor.Edited += saver.RequestSave;
        icons.Imported += icon =>
        {
            editor.ShowImportedIcon(icon.FileName);
            deck.Refresh(session.GetIconFilePath);
        };
        profile.NameEdited += saver.RequestSave;
        profile.ColorEdited += hex =>
        {
            gateway.PreviewColor(hex);
            saver.RequestSave();
        };
        profile.Loaded += (profileId, document) =>
        {
            if (document != null)
            {
                session.ShowProfile(profileId, document);
                deck.Refresh(session.GetIconFilePath);
            }
            if (deck.SelectedPosition is { } selected) editor.Show(selected);
        };
        profile.Removed += session.Clear;
        
        return new DeckApplication(new MainViewModel(activity, connection, profile, deck, editor, icons), device);
    }
}
