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
    private bool _isLoadingProfile = false;

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

    // --- НОВА СИСТЕМА ЛОГУВАННЯ ---
    private void AppLog(string message, bool isError = false)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string prefix = isError ? "[ПОМИЛКА]" : "[ІНФО]";
            string logLine = $"[{time}] {prefix} {message}\r\n";

            LogTextBox.Text += logLine;
            LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0; // Автоматична прокрутка вниз
        });
    }

    // --- АВТОПІДКЛЮЧЕННЯ (UI UPDATES) ---

    private async void HandleConnected(string portName)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = $"Статус: Підключено ({portName})";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
            SyncButton.IsEnabled = false;
            AppLog($"Підключено до порту {portName}");
            AppLog("Завантаження конфігурації з Лілки...");
        });

        // 1. Отримуємо список профілів з SD-карти
        string[] profiles = await _comService.GetProfilesListAsync();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProfileIdComboBox.ItemsSource = profiles;
            if (profiles.Length > 0)
                ProfileIdComboBox.SelectedIndex = 0;
            else
                AppLog("Готово (профілі відсутні)");

            SyncButton.IsEnabled = true;
        });
    }

    private void HandleDisconnected()
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = "Статус: Пошук пристрою...";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FFA500");
            SyncButton.IsEnabled = false;
            AppLog("Пристрій відключено. Пошук...", true);
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

            // 1. Get formatted values from UI
            string hexColor = $"#{ActiveColorPicker.Color.R:X2}{ActiveColorPicker.Color.G:X2}{ActiveColorPicker.Color.B:X2}";
            string profileName = ProfileNameTextBox.Text ?? "Profile";
            int profileId = int.TryParse(ProfileIdComboBox.SelectedItem?.ToString(), out int id) ? id : 0;

            // 2. Delegate payload building to the Data Service
            var payload = _profileData.BuildSyncPayload(profileName, hexColor);

            // 3. Setup progress tracking callbacks (Тепер використовує AppLog)
            var progress = new Progress<int>(percent => SyncProgressBar.Value = percent);
            var status = new Progress<string>(msg => AppLog(msg));

            // 4. Execute Sync
            await _comService.SyncDataAsync(profileId, payload.jsonBytes, payload.filesToSend, progress, status);

            AppLog("Синхронізація успішна!");
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
            catch (Exception ex) { AppLog($"Launch error: {ex.Message}", true); }
        });
    }

    private void HandleLogMessage(string msg) => AppLog(msg);

    private void HandleError(Exception ex)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = $"Помилка: {ex.Message}";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
            AppLog($"Синхронізацію перервано: {ex.Message}", true);
        });
    }

    // --- LIVE PREVIEW КОЛЬОРУ ---
    private void OnActiveColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_isLoadingProfile) return;

        string hexColor = $"#{e.NewColor.R:X2}{e.NewColor.G:X2}{e.NewColor.B:X2}";
        _comService.SendColorPreview(hexColor);
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
                catch (Exception ex) { AppLog($"Conversion Error: {ex.Message}", true); }
            }

            IconPathTextBox.Text = fileName;

            string cachedPath = _profileData.CacheImage(rawOutputPath, fileName);
            _profileData.UpdateConfig(_currentSelectedPosition, c =>
            {
                c.IconPath = fileName;
                c.IconFullPath = cachedPath;
            });
        }
    }

    private async void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ProfileIdComboBox.SelectedItem is not string profileIdStr) return;
        if (!int.TryParse(profileIdStr, out int profileId)) return;

        AppLog($"Завантаження Профілю {profileId}...");
        SyncButton.IsEnabled = false;

        _isLoadingProfile = true;

        try
        {
            // 2. Викачуємо config.json
            byte[]? jsonBytes = await _comService.DownloadFileAsync(profileId, "config.json");
            if (jsonBytes != null)
            {
                string jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                var config = _profileData.LoadFromJson(jsonString);

                if (config != null)
                {
                    ProfileNameTextBox.Text = config.ProfileName;

                    try { ActiveColorPicker.Color = Color.Parse(config.ActiveColor); }
                    catch { /* Ігноруємо помилки парсингу кольору */ }

                    // 3. Завантажуємо іконки, яких немає в локальному кеші
                    foreach (var kvp in config.Buttons)
                    {
                        if (!string.IsNullOrEmpty(kvp.Value.Icon))
                        {
                            string expectedCachePath = Path.Combine(_profileData.CacheDirectory, kvp.Value.Icon);
                            if (!File.Exists(expectedCachePath))
                            {
                                AppLog($"Завантаження {kvp.Value.Icon}...");
                                byte[]? iconBytes = await _comService.DownloadFileAsync(profileId, kvp.Value.Icon);
                                if (iconBytes != null)
                                {
                                    await File.WriteAllBytesAsync(expectedCachePath, iconBytes);
                                    _profileData.UpdateConfig(kvp.Key, c => c.IconFullPath = expectedCachePath);
                                }
                            }
                        }
                    }
                }
            }
            AppLog("Готово до синхронізації");
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            _isLoadingProfile = false;
            SyncButton.IsEnabled = true;

            if (!string.IsNullOrEmpty(_currentSelectedPosition))
            {
                OnDeckButtonClicked(new Button { Tag = _currentSelectedPosition }, new RoutedEventArgs());
            }
        }
    }
}
