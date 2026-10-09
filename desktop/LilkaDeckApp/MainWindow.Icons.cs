using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using LilkaDeckApp.Domain;
using LilkaDeckApp.ViewModels;

namespace LilkaDeckApp;

// Dropping a picture on a deck button: the only part of icon import that needs the view.
public partial class MainWindow
{
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles()?.ToArray();
        if (files == null || files.Length == 0 || e.Source is not Control source) return;
        
        var button = source.FindAncestorOfType<Button>(includeSelf: true);
        if (button?.Content is not DeckButtonViewModel target) return;
        
        string? inputPath = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        if (string.IsNullOrEmpty(inputPath)) return;
        
        if (_viewModel.Deck.SelectedPosition != target.Position) _viewModel.Deck.Select(target.Position);
        
        AppLog($"Обробка файлу {files[0].Name} для кнопки {target.Position.ToWireName()}...");
        await _viewModel.Icons.ImportAsync(inputPath);
    }
}
