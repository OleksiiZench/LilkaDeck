using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.Domain;

namespace LilkaDeckApp;

// Everything that puts a picture on a deck button: the file picker, drag and drop, and the gallery.
// All three end in ApplyIconAsync, so they behave the same.
public partial class MainWindow
{
    private async void OnBrowseIconClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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

        if (files.Count > 0)
        {
            await ApplyIconAsync(_currentSelectedPosition, files[0].Path.LocalPath);
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles()?.ToArray();
        if (files == null || files.Length == 0 || e.Source is not Control targetControl) return;

        Button? targetButton = targetControl.FindAncestorOfType<Button>(includeSelf: true);
        if (targetButton?.Tag is not string position) return;

        if (!DeckPositions.TryParse(position, out var target)) return;
        if (_currentSelectedPosition != position) _viewModel.Deck.Select(target);

        string? inputPath = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        if (string.IsNullOrEmpty(inputPath)) return;

        AppLog($"Обробка файлу {files[0].Name} для кнопки {position}...");
        await ApplyIconAsync(position, inputPath);
    }

    private async void OnGalleryIconSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string inputPath }) return;
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;

        AppLog($"Обрано з галереї: {Path.GetFileName(inputPath)}");
        GalleryButton.Flyout?.Hide();
        await ApplyIconAsync(_currentSelectedPosition, inputPath);
    }

    private async Task ApplyIconAsync(string position, string sourcePath)
    {
        ImportedIcon icon;
        try
        {
            icon = _profileData.ImportIcon(sourcePath);
        }
        catch (IconImportException ex)
        {
            AppLog(ex.Error == IconImportError.UnsupportedFormat
                ? "Помилка: підтримуються лише формати PNG, JPG, JPEG та RAW."
                : "Помилка: файл .raw має бути 64×64 пікселі у форматі RGB565 (8192 байти).", true);
            return;
        }
        catch (Exception ex)
        {
            AppLog($"Помилка конвертації: {ex.Message}", true);
            return;
        }

        _viewModel.Editor.ShowImportedIcon(icon.FileName);
        _profileData.UpdateConfig(position, c =>
        {
            c.IconPath = icon.FileName;
            c.IconFullPath = icon.CachedPath;
            c.NeedsUpload = true;
        });

        RefreshDeck();

        await AutoSync.SyncNowAsync();
    }
}
