using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LilkaDeckApp.Composition;
using LilkaDeckApp.Converters;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Services;
using LilkaDeckApp.ViewModels;

namespace LilkaDeckApp;

// The window only shows the view models. What is left here needs the view itself: the drop target,
// the key recorder, the scroll position of the log, and hiding the window in the tray.
public partial class MainWindow : Window
{
    private readonly DeckApplication _application;
    private readonly MainViewModel _viewModel;
    
    public bool IsRealClose { get; set; } = false;
    
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        
        _application = DeckApplication.Create(new AvaloniaFilePicker(this), new AvaloniaUiDispatcher());
        _viewModel = _application.ViewModel;
        DataContext = _viewModel;
        
        KeepNewestLogLineInView();
        ReleaseBitmapsThatAreNoLongerShown();
        
        _application.Start();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!IsRealClose)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _application.Dispose();
    }
    
    private void KeepNewestLogLineInView()
    {
        _viewModel.Activity.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActivityViewModel.LogText))
            {
                Dispatcher.UIThread.Post(() => LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0);
            }
        };
    }
    
    private void ReleaseBitmapsThatAreNoLongerShown()
    {
        _viewModel.Deck.IconsRefreshed += () => IconBitmapConverter.Instance.Retain(_viewModel.Deck.IconFilePaths);
        
        var gallery = _viewModel.Icons.Gallery;
        gallery.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IconGalleryViewModel.Items))
            {
                ThumbnailBitmapConverter.Instance.Retain(gallery.Items.Select(item => item.FilePath));
            }
        };
    }
    
    // A picture dropped on a deck button goes to that button, which is selected first.
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles()?.ToArray();
        if (files == null || files.Length == 0 || e.Source is not Control source) return;
        
        var button = source.FindAncestorOfType<Button>(includeSelf: true);
        if (button?.Content is not DeckButtonViewModel target) return;
        
        string? inputPath = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        if (string.IsNullOrEmpty(inputPath)) return;
        
        if (_viewModel.Deck.SelectedPosition != target.Position) _viewModel.Deck.Select(target.Position);
        
        _viewModel.Activity.Info($"Обробка файлу {files[0].Name} для кнопки {target.Position.ToWireName()}...");
        await _viewModel.Icons.ImportAsync(inputPath);
    }
    
    // In shortcut mode the box is a recorder: the key presses become a combination, nothing is typed.
    private void OnActionsTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        var editor = _viewModel.Editor;
        if (!editor.IsShortcutMode) return;
        
        e.Handled = true;
        
        if (e.Key is Key.Back or Key.Delete)
        {
            editor.ClearAction();
            return;
        }
        
        switch (ShortcutCapture.TryCapture(e.Key, e.KeyModifiers, out var shortcut))
        {
            case CaptureOutcome.Captured:
                editor.SetShortcut(shortcut!);
                break;
            case CaptureOutcome.Unsupported:
                _viewModel.Activity.Error($"Клавіша {e.Key} поки не підтримується прошивкою.");
                break;
        }
    }
}
