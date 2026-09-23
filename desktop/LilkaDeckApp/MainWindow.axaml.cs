using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using LilkaDeckApp.Services;

namespace LilkaDeckApp;

public partial class MainWindow : Window
{
    private string _currentSelectedPosition = "";
    private bool _isLoadingProfile = false;
    private DispatcherTimer _autoSyncTimer;
    private string _lastColor = "";
    private bool _isDesktopSyncing = false;
    private bool _pendingSync = false;

    private readonly LilkaCommunicationService _comService;
    private readonly ProfileDataService _profileData;

    public MainWindow()
    {
        InitializeComponent();

        _profileData = new ProfileDataService();
        _comService = new LilkaCommunicationService();

        // Initialize the auto-synchronization timer (1.5 seconds of silence before sending)
        _autoSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _autoSyncTimer.Tick += OnAutoSyncTimerTick;

        _comService.OnConnected += HandleConnected;
        _comService.OnDisconnected += HandleDisconnected;
        _comService.OnExecuteRequested += HandleExecuteRequest;
        _comService.OnLogMessage += HandleLogMessage;
        _comService.OnError += HandleError;

        _comService.StartAutoScanner();
    }

    private void AppLog(string message, bool isError = false)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string prefix = isError ? "[ПОМИЛКА]" : "[ІНФО]";
            string logLine = $"[{time}] {prefix} {message}\r\n";

            LogTextBox.Text += logLine;
            LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;
        });
    }

    private async void HandleConnected(string portName)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = $"Статус: Підключено ({portName})";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
            AppLog($"Підключено до порту {portName}");
            AppLog("Завантаження конфігурації з Лілки...");
        });

        string[] profiles = await _comService.GetProfilesListAsync();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProfileIdComboBox.ItemsSource = profiles;
            if (profiles.Length > 0)
                ProfileIdComboBox.SelectedIndex = 0;
            else
                AppLog("Готово (профілі відсутні)");
        });
    }

    private void HandleDisconnected()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = "Статус: Пошук пристрою...";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FFA500");
            AppLog("Пристрій відключено. Пошук...", true);
        });
    }

    // --- AUTO-SYNCHRONIZATION LOGIC ---

    private void TriggerAutoSync()
    {
        // Do not start synchronization if data is still loading or there is no connection
        if (_isLoadingProfile || !_comService.IsConnected) return;

        // Reset the timer. If the user continues typing, the countdown will start over.
        _autoSyncTimer.Stop();
        _autoSyncTimer.Start();
    }

    private async void OnAutoSyncTimerTick(object? sender, EventArgs e)
    {
        _autoSyncTimer.Stop(); // Pause the timer until the next changes
        await PerformSyncAsync();
    }

    private async Task PerformSyncAsync()
    {
        if (!_comService.IsConnected) return;

        if (_isDesktopSyncing)
        {
            _pendingSync = true; 
            return;
        }

        _isDesktopSyncing = true;

        while (true)
        {
            _pendingSync = false;

            try
            {
                SyncProgressBar.Value = 0;
                AppLog("Автосинхронізація...");

                string hexColor = $"#{ActiveColorPicker.Color.R:X2}{ActiveColorPicker.Color.G:X2}{ActiveColorPicker.Color.B:X2}";
                string profileName = ProfileNameTextBox.Text ?? "Profile";
                int profileId = int.TryParse(ProfileIdComboBox.SelectedItem?.ToString(), out int id) ? id : 0;

                var payload = _profileData.BuildSyncPayload(profileName, hexColor);
                var progress = new Progress<int>(percent => SyncProgressBar.Value = percent);
                var status = new Progress<string>(msg => AppLog(msg));

                await _comService.SyncDataAsync(profileId, payload.jsonBytes, payload.filesToSend, progress, status);
                AppLog("Збережено на пристрій!");
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            if (!_pendingSync) break; 
        }

        _isDesktopSyncing = false;
    }

    // --- SERVICE EVENT HANDLERS ---

    private void HandleExecuteRequest(string target)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try { Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }); }
            catch (Exception ex) { AppLog($"Launch error: {ex.Message}", true); }
        });
    }

    private void HandleLogMessage(string msg) => AppLog(msg);

    private void HandleError(Exception ex)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = $"Помилка: {ex.Message}";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
            AppLog($"Синхронізацію перервано: {ex.Message}", true);
        });
    }

    // --- UI EVENT HANDLERS ---

    private void OnProfileNameChanged(object? sender, TextChangedEventArgs e)
    {
        // The timer will start only if the cursor is physically in this field
        if (_isLoadingProfile || !ProfileNameTextBox.IsFocused) return;
        TriggerAutoSync();
    }

    private void OnActiveColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_isLoadingProfile) return;

        string hexColor = $"#{e.NewColor.R:X2}{e.NewColor.G:X2}{e.NewColor.B:X2}";
        
        // Handle window redraw events (ignore if the color hasn't changed)
        if (hexColor == _lastColor) return;
        _lastColor = hexColor;

        _comService.SendColorPreview(hexColor);
        TriggerAutoSync();
    }

    private void OnDeckButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string position)
        {
            _currentSelectedPosition = position;
            ButtonSettingsPanel.IsEnabled = true;
            SelectedButtonLabel.Text = $"Редагування: {position}";

            var config = _profileData.GetConfig(position);

            IconPathTextBox.Text = config.IconPath;

            // We're temporarily unsubscribing so that programmatic changes to the text don't trigger auto-syncing
            ActionsTextBox.TextChanged -= OnActionsTextChanged;
            ActionsTextBox.Text = config.Actions;
            ActionsTextBox.TextChanged += OnActionsTextChanged;

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
            TriggerAutoSync();
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
        if (_isLoadingProfile || !ActionsTextBox.IsFocused) return;

        if (!string.IsNullOrEmpty(_currentSelectedPosition) && sender is TextBox tb)
        {
            _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = tb.Text ?? "");
            TriggerAutoSync();
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

            // --- INSTANT SYNCHRONIZATION FOR ICONS ---
            _autoSyncTimer.Stop();
            await PerformSyncAsync();
        }
    }

    private async void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ProfileIdComboBox.SelectedItem is not string profileIdStr) return;
        if (!int.TryParse(profileIdStr, out int profileId)) return;

        AppLog($"Завантаження Профілю {profileId}...");

        _isLoadingProfile = true;

        try
        {
            byte[]? jsonBytes = await _comService.DownloadFileAsync(profileId, "config.json");
            if (jsonBytes != null)
            {
                string jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                var config = _profileData.LoadFromJson(jsonString);

                if (config != null)
                {
                    // Temporarily unsubscribe from the event to avoid incorrect autosynchronization
                    ProfileNameTextBox.TextChanged -= OnProfileNameChanged;
                    ProfileNameTextBox.Text = config.ProfileName;
                    ProfileNameTextBox.TextChanged += OnProfileNameChanged;

                    try 
                    { 
                        _lastColor = config.ActiveColor;
                        ActiveColorPicker.Color = Color.Parse(config.ActiveColor); 
                    }
                    catch { }

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
            AppLog("Готово до редагування");
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            _isLoadingProfile = false;

            if (!string.IsNullOrEmpty(_currentSelectedPosition))
            {
                OnDeckButtonClicked(new Button { Tag = _currentSelectedPosition }, new RoutedEventArgs());
            }
        }
    }
}
