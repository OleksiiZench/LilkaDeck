using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using LilkaDeckApp.Models;
using LilkaDeckApp.Services;

namespace LilkaDeckApp;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, ButtonConfig> _deckConfigs = new();
    private string _currentSelectedPosition = "";

    // Injected communication service
    private readonly LilkaCommunicationService _comService;

    public MainWindow()
    {
        InitializeComponent();

        _comService = new LilkaCommunicationService();
        _comService.OnExecuteRequested += HandleExecuteRequest;
        _comService.OnLogMessage += HandleLogMessage;
        _comService.OnError += HandleError;

        // Initialize empty configurations
        string[] positions = { "LeftUp", "LeftLeft", "LeftRight", "LeftDown", "RightUp", "RightLeft", "RightRight", "RightDown" };
        foreach (var pos in positions)
        {
            _deckConfigs[pos] = new ButtonConfig();
        }

        LoadAvailablePorts();
    }

    private void LoadAvailablePorts()
    {
        string[] ports = SerialPort.GetPortNames();
        ComPortComboBox.ItemsSource = ports;
        if (ports.Length > 0) ComPortComboBox.SelectedIndex = 0;
    }

    private void OnRefreshPortsClicked(object? sender, RoutedEventArgs e) => LoadAvailablePorts();

    private void OnConnectButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_comService.IsConnected)
        {
            _comService.Disconnect();
            UpdateConnectionUI(false);
        }
        else if (ComPortComboBox.SelectedItem is string portName)
        {
            try
            {
                _comService.Connect(portName);
                UpdateConnectionUI(true, portName);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }
    }

    private void UpdateConnectionUI(bool isConnected, string portName = "")
    {
        if (isConnected)
        {
            ConnectButton.Content = "Відключити";
            ConnectButton.Background = SolidColorBrush.Parse("#FF5252");
            ConnectionStatusText.Text = $"Статус: Підключено ({portName})";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
            SyncButton.IsEnabled = true;
            SyncStatusText.Text = "Готово до синхронізації";
        }
        else
        {
            ConnectButton.Content = "Підключити";
            ConnectButton.Background = SolidColorBrush.Parse("#4CAF50");
            ConnectionStatusText.Text = "Статус: Відключено";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
            SyncButton.IsEnabled = false;
            SyncStatusText.Text = "Очікування підключення...";
        }
    }

    private async void OnSyncButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (!_comService.IsConnected) return;

        try
        {
            SyncButton.IsEnabled = false;
            SyncProgressBar.Value = 0;
            SyncStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");

            // Prepare Configuration
            string hexColor = $"#{ActiveColorPicker.Color.R:X2}{ActiveColorPicker.Color.G:X2}{ActiveColorPicker.Color.B:X2}";
            var output = new OutputConfig
            {
                ProfileName = ProfileNameTextBox.Text ?? "Profile",
                ActiveColor = hexColor
            };
            var filesToSend = new Dictionary<string, string>();

            foreach (var kvp in _deckConfigs)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value.IconPath) || !string.IsNullOrWhiteSpace(kvp.Value.Actions))
                {
                    var actionList = kvp.Value.ActionType == "shortcut"
                        ? kvp.Value.Actions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                        : new List<string> { kvp.Value.Actions.Trim() };

                    output.Buttons[kvp.Key] = new OutputButton
                    {
                        Icon = kvp.Value.IconPath,
                        Type = kvp.Value.ActionType,
                        Action = actionList
                    };

                    if (!string.IsNullOrWhiteSpace(kvp.Value.IconFullPath) && File.Exists(kvp.Value.IconFullPath))
                    {
                        filesToSend[kvp.Value.IconPath] = kvp.Value.IconFullPath;
                    }
                }
            }

            string jsonString = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
            int profileId = (int)(ProfileIdSpinner.Value ?? 0);

            // Progress handlers to update UI from background tasks
            var progress = new Progress<int>(percent => SyncProgressBar.Value = percent);
            var status = new Progress<string>(msg => SyncStatusText.Text = msg);

            // Execute Sync via Service
            await _comService.SyncDataAsync(profileId, jsonBytes, filesToSend, progress, status);

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

            var config = _deckConfigs[position];
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
            _deckConfigs[_currentSelectedPosition].ActionType = type;
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
            _deckConfigs[_currentSelectedPosition].Actions = tb.Text ?? "";
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
                try { ImageConverter.ConvertToRgb565Raw(inputPath, rawOutputPath); fileName = Path.GetFileName(rawOutputPath); }
                catch (Exception ex) { Console.WriteLine($"Conversion Error: {ex.Message}"); }
            }

            IconPathTextBox.Text = fileName;
            _deckConfigs[_currentSelectedPosition].IconPath = fileName;
            _deckConfigs[_currentSelectedPosition].IconFullPath = rawOutputPath;
        }
    }

    // Temporary keep to satisfy XAML until removed in Card 2
    private void OnGenerateJsonClicked(object? sender, RoutedEventArgs e) { /* Placeholder */ }
}
