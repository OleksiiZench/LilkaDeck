using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;

namespace LilkaDeckApp;

public class ButtonConfig
{
    public string IconPath { get; set; } = "";
    public string ActionType { get; set; } = "shortcut";
    public string Actions { get; set; } = "";
}

public partial class MainWindow : Window
{
    private readonly Dictionary<string, ButtonConfig> _deckConfigs = new();
    private string _currentSelectedPosition = "";
    
    // NEW: Object for COM port communication
    private SerialPort? _serialPort;

    public MainWindow()
    {
        InitializeComponent();

        string[] positions = { "LeftUp", "LeftLeft", "LeftRight", "LeftDown", "RightUp", "RightLeft", "RightRight", "RightDown" };
        foreach (var pos in positions)
        {
            _deckConfigs[pos] = new ButtonConfig();
        }

        // Load available COM ports at startup
        LoadAvailablePorts();
    }

    // --- NEW SERIAL PORT LOGIC ---

    private void LoadAvailablePorts()
    {
        string[] ports = SerialPort.GetPortNames();
        ComPortComboBox.ItemsSource = ports;
        
        if (ports.Length > 0)
        {
            ComPortComboBox.SelectedIndex = 0;
        }
    }

    private void OnRefreshPortsClicked(object? sender, RoutedEventArgs e)
    {
        LoadAvailablePorts();
    }

    private void OnConnectButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            // Disconnect
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
            
            ConnectButton.Content = "Підключити";
            ConnectButton.Background = SolidColorBrush.Parse("#4CAF50");
            ConnectionStatusText.Text = "Статус: Відключено";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
        }
        else
        {
            // Connect
            if (ComPortComboBox.SelectedItem is string portName)
            {
                try
                {
                    _serialPort = new SerialPort(portName, 115200);
                    _serialPort.DataReceived += OnSerialDataReceived;
                    _serialPort.Open();

                    ConnectButton.Content = "Відключити";
                    ConnectButton.Background = SolidColorBrush.Parse("#FF5252");
                    ConnectionStatusText.Text = $"Статус: Підключено ({portName})";
                    ConnectionStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
                }
                catch (Exception ex)
                {
                    ConnectionStatusText.Text = $"Помилка: {ex.Message}";
                    ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
                }
            }
        }
    }

    // Background COM port listener (runs on a separate thread!)
    private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            // Read the string from Lilka
            string data = _serialPort.ReadLine().Trim();
            
            // If this is a launch command
            if (data.StartsWith("EXECUTE:"))
            {
                string target = data.Substring(8).Trim();
                
                // Launch via the system (UseShellExecute is mandatory for URLs and general files)
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Serial Error: {ex.Message}");
        }
    }

    // --- OLD UI LOGIC ---

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
            ActionHintTextBlock.Text = "Дії (через кому, напр: CTRL,SHIFT,M):";
            ActionsTextBox.PlaceholderText = "CTRL, ALT, T";
        }
        else if (type == "launch")
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
            Title = "Оберіть картинку (PNG, JPG)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Зображення (PNG, JPG)") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } },
                new FilePickerFileType("Готові RAW") { Patterns = new[] { "*.raw", "*.rgb565" } }
            }
        });

        if (files.Count >= 1)
        {
            string inputPath = files[0].Path.LocalPath;
            string fileName = files[0].Name;

            if (!fileName.EndsWith(".raw") && !fileName.EndsWith(".rgb565"))
            {
                string rawFileName = Path.GetFileNameWithoutExtension(fileName) + ".raw";
                string rawOutputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, rawFileName);
                
                try
                {
                    ImageConverter.ConvertToRgb565Raw(inputPath, rawOutputPath);
                    fileName = rawFileName;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Conversion Error: {ex.Message}");
                }
            }

            IconPathTextBox.Text = fileName;
            _deckConfigs[_currentSelectedPosition].IconPath = fileName;
        }
    }

    private async void OnGenerateJsonClicked(object? sender, RoutedEventArgs e)
    {
        var output = new OutputConfig
        {
            ProfileName = ProfileNameTextBox.Text ?? "Profile"
        };

        foreach (var kvp in _deckConfigs)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value.IconPath) || !string.IsNullOrWhiteSpace(kvp.Value.Actions))
            {
                var actionList = new List<string>();

                if (kvp.Value.ActionType == "shortcut")
                {
                    actionList = kvp.Value.Actions
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
                else if (kvp.Value.ActionType == "launch" && !string.IsNullOrWhiteSpace(kvp.Value.Actions))
                {
                    actionList.Add(kvp.Value.Actions.Trim());
                }

                output.Buttons[kvp.Key] = new OutputButton
                {
                    Icon = kvp.Value.IconPath,
                    Type = kvp.Value.ActionType,
                    Action = actionList
                };
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(output, options);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Зберегти конфігурацію",
                SuggestedFileName = "config.json",
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON File") { Patterns = new[] { "*.json" } } }
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var streamWriter = new StreamWriter(stream);
                await streamWriter.WriteAsync(jsonString);
            }
        }
    }
}

// Models
public class OutputConfig
{
    [JsonPropertyName("profileName")]
    public string ProfileName { get; set; } = "Profile";

    [JsonPropertyName("buttons")]
    public Dictionary<string, OutputButton> Buttons { get; set; } = new();
}

public class OutputButton
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "shortcut";

    [JsonPropertyName("action")]
    public List<string> Action { get; set; } = new();
}
