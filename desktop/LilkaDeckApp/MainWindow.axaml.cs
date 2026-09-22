using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using System;
using System.IO;
using System.Diagnostics;
using LilkaDeckApp.Services;

namespace LilkaDeckApp;

public partial class MainWindow : Window
{
    private string _currentSelectedPosition = "";
    
    private readonly LilkaCommunicationService _comService;
    private readonly ProfileDataService _profileData;

    public MainWindow()
    {
        InitializeComponent();

        _profileData = new ProfileDataService();
        _comService = new LilkaCommunicationService();
        
        // Підписуємося на події автопідключення
        _comService.OnConnected += HandleConnected;
        _comService.OnDisconnected += HandleDisconnected;
        
        _comService.OnExecuteRequested += HandleExecuteRequest;
        _comService.OnLogMessage += HandleLogMessage;
        _comService.OnError += HandleError;

        // ВАЖЛИВО: Запускаємо фоновий сканер при старті додатку
        _comService.StartAutoScanner();
    }

    // --- АВТОПІДКЛЮЧЕННЯ (UI UPDATES) ---

    private void HandleConnected(string portName)
    {
        // Всі оновлення UI повинні виконуватися в головному потоці
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Якщо у тебе ще залишилися ComboBox або ConnectButton у XAML, ти можеш їх сховати/заблокувати тут.
            ConnectionStatusText.Text = $"Статус: Підключено ({portName})";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
            SyncButton.IsEnabled = true;
            SyncStatusText.Text = "Готово до синхронізації";
        });
    }

    private void HandleDisconnected()
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = "Статус: Пошук пристрою...";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FFA500"); // Оранжевий колір пошуку
            SyncButton.IsEnabled = false;
            SyncStatusText.Text = "Очікування підключення...";
        });
    }

    // --- SYNCHRONIZATION ---

    private async void OnSyncButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (!_comService.IsConnected) return;

        try
        {
            SyncButton.IsEnabled = false;
            SyncProgressBar.Value = 0;
            SyncStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");

            // 1. Get formatted values from UI
            string hexColor = $"#{ActiveColorPicker.Color.R:X2}{ActiveColorPicker.Color.G:X2}{ActiveColorPicker.Color.B:X2}";
            string profileName = ProfileNameTextBox.Text ?? "Profile";
            int profileId = (int)(ProfileIdSpinner.Value ?? 0);

            // 2. Delegate payload building to the Data Service
            var payload = _profileData.BuildSyncPayload(profileName, hexColor);

            // 3. Setup progress tracking callbacks
            var progress = new Progress<int>(percent => SyncProgressBar.Value = percent);
            var status = new Progress<string>(msg => SyncStatusText.Text = msg);

            // 4. Execute Sync
            await _comService.SyncDataAsync(profileId, payload.jsonBytes, payload.filesToSend, progress, status);

            SyncStatusText.Text = "Синхронізація успішна!";
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            SyncButton.IsEnabled = true;
        }
    }

    // --- SERVICE EVENT HANDLERS ---

    private void HandleExecuteRequest(string target)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            try { Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }); }
            catch (Exception ex) { Console.WriteLine($"Launch error: {ex.Message}"); }
        });
    }

    private void HandleLogMessage(string msg) => Console.WriteLine(msg);

    private void HandleError(Exception ex)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = $"Помилка: {ex.Message}";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
            SyncStatusText.Text = "Синхронізацію перервано";
        });
    }

    // --- UI EVENT HANDLERS (Deck buttons, ComboBoxes, File Picker) ---

    private void OnDeckButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string position)
        {
            _currentSelectedPosition = position;
            ButtonSettingsPanel.IsEnabled = true;
            SelectedButtonLabel.Text = $"Редагування: {position}";

            // Fetch state from Data Service
            var config = _profileData.GetConfig(position);

            IconPathTextBox.Text = config.IconPath;
            ActionsTextBox.Text = config.Actions;

            ActionTypeComboBox.SelectionChanged -= OnActionTypeChanged;
            ActionTypeComboBox.SelectedIndex = config.ActionType == "launch" ? 1 : 0;
            UpdateActionHint(config.ActionType);
            ActionTypeComboBox.SelectionChanged += OnActionTypeChanged;
        }
    }

    private void OnActionTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;
        if (ActionTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string type)
        {
            _profileData.UpdateConfig(_currentSelectedPosition, c => c.ActionType = type);
            UpdateActionHint(type);
        }
    }

    private void UpdateActionHint(string type)
    {
        if (type == "shortcut")
        {
            ActionHintTextBlock.Text = "Дії (через кому):";
            ActionsTextBox.PlaceholderText = "CTRL, ALT, T";
        }
        else
        {
            ActionHintTextBlock.Text = "Шлях до програми або URL:";
            ActionsTextBox.PlaceholderText = @"C:\Apps\Discord.exe або https://youtube.com";
        }
    }

    private void OnActionsTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentSelectedPosition) && sender is TextBox tb)
        {
            _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = tb.Text ?? "");
        }
    }

    private async void OnBrowseIconClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Оберіть картинку",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.raw" } } }
        });

        if (files.Count > 0)
        {
            string inputPath = files[0].Path.LocalPath;
            string fileName = files[0].Name;
            string rawOutputPath = inputPath;

            if (!fileName.EndsWith(".raw"))
            {
                rawOutputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, Path.GetFileNameWithoutExtension(fileName) + ".raw");
                try
                {
                    ImageConverter.ConvertToRgb565Raw(inputPath, rawOutputPath);
                    fileName = Path.GetFileName(rawOutputPath);
                }
                catch (Exception ex) { Console.WriteLine($"Conversion Error: {ex.Message}"); }
            }

            IconPathTextBox.Text = fileName;

            // Update state in Data Service
            _profileData.UpdateConfig(_currentSelectedPosition, c =>
            {
                c.IconPath = fileName;
                c.IconFullPath = rawOutputPath;
            });
        }
    }

    // Temporary keep to satisfy XAML until removed in Card 2
    private void OnGenerateJsonClicked(object? sender, RoutedEventArgs e) { /* Placeholder */ }
}
