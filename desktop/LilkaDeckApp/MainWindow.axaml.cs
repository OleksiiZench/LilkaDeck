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
using Avalonia.VisualTree;

using LilkaDeckApp.Services;


namespace LilkaDeckApp;

public partial class MainWindow : Window
{
    private string _currentSelectedPosition = "";
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

        InitializeShell();

        _comService.OnConnected += HandleConnected;
        _comService.OnDisconnected += HandleDisconnected;
        _comService.OnExecuteRequested += HandleExecuteRequest;
        _comService.OnLogMessage += HandleLogMessage;
        _comService.OnError += HandleError;

        _comService.StartAutoScanner();
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
        if (_viewModel.Profile.IsLoading || !ActionsTextBox.IsFocused) return;

        if (!string.IsNullOrEmpty(_currentSelectedPosition) && sender is TextBox tb)
        {
            _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = tb.Text ?? "");
            TriggerAutoSync();
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

    private void OnMediaButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;
        if (sender is not Button btn || btn.Tag is not string mediaToken) return;

        ActionsTextBox.Text = mediaToken;
        _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = mediaToken);
        TriggerAutoSync();
    }
}
