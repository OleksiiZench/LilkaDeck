using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Reflection;
using System.Linq;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    private string[] _availableProfiles = Array.Empty<string>();
    private int _currentProfileIndex = 0;

    private readonly LilkaCommunicationService _comService;
    private readonly ProfileDataService _profileData;
    private Dictionary<string, Button> _deckButtons;

    public bool IsRealClose { get; set; } = false;

    private readonly Dictionary<string, string> _defaultButtonTexts = new()
    {
        {"LeftUp", "UP"}, {"LeftLeft", "LEFT"}, {"LeftRight", "RIGHT"}, {"LeftDown", "DOWN"},
        {"RightUp", "C"}, {"RightLeft", "D"}, {"RightRight", "A"}, {"RightDown", "B"}
    };

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);

        _deckButtons = new Dictionary<string, Button>
        {
            { "LeftUp", BtnLeftUp }, { "LeftLeft", BtnLeftLeft }, { "LeftRight", BtnLeftRight }, { "LeftDown", BtnLeftDown },
            { "RightUp", BtnRightUp }, { "RightLeft", BtnRightLeft }, { "RightRight", BtnRightRight }, { "RightDown", BtnRightDown }
        };

        _profileData = new ProfileDataService();
        _comService = new LilkaCommunicationService();

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
            AddProfileButton.IsEnabled = true;
            AppLog($"Підключено до порту {portName}");
            AppLog("Завантаження конфігурації з Лілки...");
        });

        await RefreshProfileStateAsync();
    }

    private void HandleDisconnected()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatusText.Text = "Статус: Пошук пристрою...";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FFA500");
            AddProfileButton.IsEnabled = false;
            DeleteProfileButton.IsEnabled = false;
            AppLog("Пристрій відключено. Пошук...", true);
        });
    }

    private async Task RefreshProfileStateAsync(string targetProfileId = "0")
    {
        _availableProfiles = await _comService.GetProfilesListAsync();

        if (_availableProfiles.Length > 0)
        {
            _currentProfileIndex = Array.IndexOf(_availableProfiles, targetProfileId);
            if (_currentProfileIndex == -1) _currentProfileIndex = 0;

            await LoadActiveProfileAsync();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ActiveProfileTitle.Text = "Профілі відсутні";
                DeleteProfileButton.IsEnabled = false;
            });
        }
    }

    private void OnPrevProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (_availableProfiles.Length == 0 || _isLoadingProfile) return;
        _currentProfileIndex = (_currentProfileIndex == 0) ? (_availableProfiles.Length - 1) : (_currentProfileIndex - 1);
        _ = LoadActiveProfileAsync();
    }

    private void OnNextProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (_availableProfiles.Length == 0 || _isLoadingProfile) return;
        _currentProfileIndex = (_currentProfileIndex + 1) % _availableProfiles.Length;
        _ = LoadActiveProfileAsync();
    }

    private async Task LoadActiveProfileAsync()
    {
        if (_availableProfiles.Length == 0) return;

        string profileIdStr = _availableProfiles[_currentProfileIndex];
        if (!int.TryParse(profileIdStr, out int profileId)) return;

        AppLog($"Завантаження Профілю {profileId}...");
        _isLoadingProfile = true;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ActiveProfileTitle.Text = $"Профіль {profileId}";
            DeleteProfileButton.IsEnabled = _availableProfiles.Length > 1;
        });

        try
        {
            byte[]? jsonBytes = await _comService.DownloadFileAsync(profileId, "config.json");
            if (jsonBytes != null)
            {
                string jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                var config = _profileData.LoadFromJson(jsonString);

                if (config != null)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ProfileNameTextBox.TextChanged -= OnProfileNameChanged;
                        ProfileNameTextBox.Text = config.ProfileName;
                        ProfileNameTextBox.TextChanged += OnProfileNameChanged;

                        ActiveProfileTitle.Text = $"Профіль {profileId}: {config.ProfileName}";

                        try
                        {
                            _lastColor = config.ActiveColor;
                            ActiveColorPicker.Color = Color.Parse(config.ActiveColor);
                        }
                        catch { }
                    });

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
                    UpdateDeckVisuals();
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

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!string.IsNullOrEmpty(_currentSelectedPosition))
                {
                    OnDeckButtonClicked(new Button { Tag = _currentSelectedPosition }, new RoutedEventArgs());
                }
            });
        }
    }

    private async void OnAddProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (!_comService.IsConnected || _isLoadingProfile) return;

        AppLog("Створення нового профілю...");
        AddProfileButton.IsEnabled = false;
        DeleteProfileButton.IsEnabled = false;

        try
        {
            int? newId = await _comService.CreateProfileAsync();
            if (newId.HasValue)
            {
                AppLog($"Профіль {newId.Value} успішно створено!");
                await RefreshProfileStateAsync(newId.Value.ToString());
            }
            else
            {
                AppLog("Помилка: Лілка не відповіла на створення профілю.", true);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            AddProfileButton.IsEnabled = true;
        }
    }

    private async void OnDeleteProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (!_comService.IsConnected || _isLoadingProfile || _availableProfiles.Length == 0) return;

        string profileIdStr = _availableProfiles[_currentProfileIndex];
        if (!int.TryParse(profileIdStr, out int profileId)) return;

        AppLog($"Видалення профілю {profileId}...");
        AddProfileButton.IsEnabled = false;
        DeleteProfileButton.IsEnabled = false;

        try
        {
            bool success = await _comService.DeleteProfileAsync(profileId);
            if (success)
            {
                AppLog("Профіль успішно видалено!");
                _profileData.ClearState();
                await RefreshProfileStateAsync("0");
            }
            else
            {
                AppLog("Помилка при видаленні профілю.", true);
                DeleteProfileButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
            DeleteProfileButton.IsEnabled = true;
        }
        finally
        {
            AddProfileButton.IsEnabled = true;
        }
    }

    // --- AUTO-SYNCHRONIZATION LOGIC ---

    private void TriggerAutoSync()
    {
        if (_isLoadingProfile || !_comService.IsConnected) return;
        _autoSyncTimer.Stop();
        _autoSyncTimer.Start();
    }

    private async void OnAutoSyncTimerTick(object? sender, EventArgs e)
    {
        _autoSyncTimer.Stop();
        await PerformSyncAsync();
    }

    private async Task PerformSyncAsync()
    {
        if (!_comService.IsConnected || _availableProfiles.Length == 0) return;

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

                string profileIdStr = _availableProfiles[_currentProfileIndex];
                int profileId = int.TryParse(profileIdStr, out int id) ? id : 0;

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ActiveProfileTitle.Text = $"Профіль {profileId}: {profileName}";
                });

                var payload = _profileData.BuildSyncPayload(profileName, hexColor);
                var progress = new Progress<int>(percent => SyncProgressBar.Value = percent);
                var status = new Progress<string>(msg => AppLog(msg));

                await _comService.SyncDataAsync(profileId, payload.jsonBytes, payload.filesToSend, progress, status);
                AppLog("Збережено на пристрій!");

                foreach (var pos in new[] { "LeftUp", "LeftLeft", "LeftRight", "LeftDown", "RightUp", "RightLeft", "RightRight", "RightDown" })
                {
                    _profileData.UpdateConfig(pos, c => c.NeedsUpload = false);
                }
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
        if (_isLoadingProfile || !ProfileNameTextBox.IsFocused) return;
        TriggerAutoSync();
    }

    private void OnActiveColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_isLoadingProfile) return;

        string hexColor = $"#{e.NewColor.R:X2}{e.NewColor.G:X2}{e.NewColor.B:X2}";

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

            ActionsTextBox.TextChanged -= OnActionsTextChanged;
            ActionsTextBox.Text = config.ActionType == "shortcut"
                ? config.Actions.Replace(", ", " + ")
                : config.Actions;
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
            ActionHintTextBlock.Text = "Комбінація клавіш або медіа-дія:";
            ActionsTextBox.PlaceholderText = "Клікніть і натисніть комбінацію...";
            ActionsTextBox.IsReadOnly = true;
            MediaButtonsPanel.IsVisible = true;
        }
        else
        {
            ActionHintTextBlock.Text = "Шлях до програми або URL:";
            ActionsTextBox.PlaceholderText = @"C:\Apps\Discord.exe або https://youtube.com";
            ActionsTextBox.IsReadOnly = false;
            MediaButtonsPanel.IsVisible = false;
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

    private void OnActionsTextBoxKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;

        var config = _profileData.GetConfig(_currentSelectedPosition);
        if (config.ActionType != "shortcut") return;

        e.Handled = true;

        if (e.Key == Avalonia.Input.Key.Back || e.Key == Avalonia.Input.Key.Delete)
        {
            ActionsTextBox.Text = "";
            _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = "");
            TriggerAutoSync();
            return;
        }

        if (KeyCaptureMap.ModifierKeys.Contains(e.Key)) return;

        AppLog($"DEBUG KeyDown: Key={e.Key}, Modifiers={e.KeyModifiers}");

        if (!KeyCaptureMap.Map.TryGetValue(e.Key, out string? mainToken))
        {
            AppLog($"Клавіша {e.Key} поки не підтримується прошивкою.", true);
            return;
        }

        var tokens = new System.Collections.Generic.List<string>();
        if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control)) tokens.Add("CTRL");
        if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift)) tokens.Add("SHIFT");
        if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt)) tokens.Add("ALT");
        if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Meta)) tokens.Add("GUI");
        tokens.Add(mainToken);

        string stored = string.Join(", ", tokens);
        string display = string.Join(" + ", tokens);

        ActionsTextBox.Text = display;
        _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = stored);
        TriggerAutoSync();
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
                c.NeedsUpload = true;
            });

            UpdateDeckVisuals();

            _autoSyncTimer.Stop();
            await PerformSyncAsync();
        }
    }

    private void UpdateDeckVisuals()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var kvp in _deckButtons)
            {
                string pos = kvp.Key;
                Button btn = kvp.Value;
                var config = _profileData.GetConfig(pos);

                if (!string.IsNullOrEmpty(config.IconFullPath) && File.Exists(config.IconFullPath))
                {
                    var bitmap = ImageConverter.DecodeRgb565RawToBitmap(config.IconFullPath);
                    if (bitmap != null)
                    {
                        var deckImage = new Avalonia.Controls.Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.UniformToFill
                        };
                        
                        RenderOptions.SetBitmapInterpolationMode(deckImage, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);

                        btn.Content = new Avalonia.Controls.Border
                        {
                            CornerRadius = new Avalonia.CornerRadius(4),
                            ClipToBounds = true,
                            Child = deckImage
                        };
                        continue;
                    }
                }

                btn.Content = _defaultButtonTexts[pos];
            }
        });
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

    // --- DRAG AND DROP LOGIC ---
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles()?.ToArray();

        if (files != null && files.Length > 0 && e.Source is Control targetControl)
        {
            Button? targetButton = targetControl as Button ?? targetControl.Parent as Button;

            if (targetButton != null && targetButton.Tag is string position)
            {
                if (_currentSelectedPosition != position)
                {
                    OnDeckButtonClicked(targetButton, new RoutedEventArgs());
                }

                string? inputPath = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
                if (string.IsNullOrEmpty(inputPath)) return;

                string fileName = files[0].Name;
                string extension = Path.GetExtension(fileName).ToLower();

                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".raw")
                {
                    AppLog("Помилка: підтримуються лише формати PNG, JPG, JPEG та RAW.", true);
                    return;
                }

                AppLog($"Обробка файлу {fileName} для кнопки {position}...");
                string rawOutputPath = inputPath;

                if (extension != ".raw")
                {
                    rawOutputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, Path.GetFileNameWithoutExtension(fileName) + ".raw");
                    try
                    {
                        ImageConverter.ConvertToRgb565Raw(inputPath, rawOutputPath);
                        fileName = Path.GetFileName(rawOutputPath);
                    }
                    catch (Exception ex)
                    {
                        AppLog($"Помилка конвертації: {ex.Message}", true);
                        return;
                    }
                }

                IconPathTextBox.Text = fileName;

                string cachedPath = _profileData.CacheImage(rawOutputPath, fileName);

                _profileData.UpdateConfig(position, c =>
                {
                    c.IconPath = fileName;
                    c.IconFullPath = cachedPath;
                    c.NeedsUpload = true;
                });

                UpdateDeckVisuals();

                _autoSyncTimer.Stop();
                await PerformSyncAsync();
            }
        }
    }
    
    // --- GALLERY LOGIC ---

    private void OnGalleryClicked(object? sender, RoutedEventArgs e)
    {
        GalleryWrapPanel.Children.Clear();

        try
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string galleryDir = Path.Combine(appDir, "assets", "standard_icons");

            if (!Directory.Exists(galleryDir))
            {
                Directory.CreateDirectory(galleryDir);
                AppLog("Створено папку для галереї: " + galleryDir);
                return;
            }

            var files = Directory.GetFiles(galleryDir)
                                 .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                             f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                 .ToArray();

            if (files.Length == 0)
            {
                AppLog("Галерея порожня. Додайте картинки в папку Assets/StandardIcons.");
                return;
            }

            foreach (string file in files)
            {
                var bitmap = new Bitmap(file);
                
                var image = new Avalonia.Controls.Image 
                { 
                    Source = bitmap,
                    Stretch = Stretch.Uniform 
                };

                RenderOptions.SetBitmapInterpolationMode(image, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);

                var btn = new Button
                {
                    Content = image,
                    Width = 60,
                    Height = 60,
                    Margin = new Avalonia.Thickness(2),
                    Padding = new Avalonia.Thickness(6),
                    Tag = file,
                    Background = SolidColorBrush.Parse("#333")
                };

                btn.Click += OnGalleryIconSelected;
                
                GalleryWrapPanel.Children.Add(btn);
            }
        }
        catch (Exception ex)
        {
            AppLog($"Помилка завантаження галереї: {ex.Message}", true);
        }
    }

    private async void OnGalleryIconSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string inputPath)
        {
            if (string.IsNullOrEmpty(_currentSelectedPosition)) return;

            string fileName = Path.GetFileName(inputPath);
            AppLog($"Обрано з галереї: {fileName}");

            string rawOutputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, Path.GetFileNameWithoutExtension(fileName) + ".raw");

            try
            {
                ImageConverter.ConvertToRgb565Raw(inputPath, rawOutputPath);
                fileName = Path.GetFileName(rawOutputPath);

                IconPathTextBox.Text = fileName;

                string cachedPath = _profileData.CacheImage(rawOutputPath, fileName);

                _profileData.UpdateConfig(_currentSelectedPosition, c =>
                {
                    c.IconPath = fileName;
                    c.IconFullPath = cachedPath;
                    c.NeedsUpload = true;
                });

                UpdateDeckVisuals();

                GalleryButton.Flyout?.Hide();

                _autoSyncTimer.Stop();
                await PerformSyncAsync();
            }
            catch (Exception ex)
            {
                AppLog($"Помилка застосування іконки: {ex.Message}", true);
            }
        }
    }

    private void OnMediaButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;
        if (sender is not Button btn || btn.Tag is not string mediaToken) return;

        ActionsTextBox.Text = mediaToken;
        _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = mediaToken);
        TriggerAutoSync();
    }
}
