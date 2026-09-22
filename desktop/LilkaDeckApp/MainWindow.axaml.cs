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
using System.Threading.Tasks;

namespace LilkaDeckApp;

/// <summary>
/// Local model for storing the settings of each button in the desktop application's memory.
/// </summary>
public class ButtonConfig
{
    public string IconPath { get; set; } = "";
    
    // Full path to the generated .raw file on the computer. 
    // Used for reading bytes during cable synchronization.
    public string IconFullPath { get; set; } = ""; 
    
    public string ActionType { get; set; } = "shortcut";
    public string Actions { get; set; } = "";
}

public partial class MainWindow : Window
{
    // Dictionary for storing button configurations, where the key is the physical button position (e.g., "LeftUp").
    private readonly Dictionary<string, ButtonConfig> _deckConfigs = new();
    
    // The current button selected by the user for editing in the UI.
    private string _currentSelectedPosition = "";
    
    // Object for working with the virtual COM port (USB CDC).
    private SerialPort? _serialPort;
    
    // Helper object to convert events (DataReceived) into asynchronous tasks (Task).
    // Allows methods to wait for a specific response (ACK) from Lilka without blocking the UI.
    private TaskCompletionSource<string>? _ackTcs;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize empty configurations for all available macro pad buttons
        string[] positions = { "LeftUp", "LeftLeft", "LeftRight", "LeftDown", "RightUp", "RightLeft", "RightRight", "RightDown" };
        foreach (var pos in positions)
        {
            _deckConfigs[pos] = new ButtonConfig();
        }

        LoadAvailablePorts();
    }

    /// <summary>
    /// Scans the system for active COM ports and adds them to the dropdown list.
    /// </summary>
    private void LoadAvailablePorts()
    {
        string[] ports = SerialPort.GetPortNames();
        ComPortComboBox.ItemsSource = ports;
        if (ports.Length > 0) ComPortComboBox.SelectedIndex = 0;
    }

    private void OnRefreshPortsClicked(object? sender, RoutedEventArgs e) => LoadAvailablePorts();

    /// <summary>
    /// Handles connecting and disconnecting from the microcontroller via the Serial port.
    /// </summary>
    private void OnConnectButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            // Disconnection logic
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
            
            ConnectButton.Content = "Підключити";
            ConnectButton.Background = SolidColorBrush.Parse("#4CAF50");
            ConnectionStatusText.Text = "Статус: Відключено";
            ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
            SyncButton.IsEnabled = false;
            SyncStatusText.Text = "Очікування підключення...";
        }
        else
        {
            // Connection logic
            if (ComPortComboBox.SelectedItem is string portName)
            {
                try
                {
                    _serialPort = new SerialPort(portName, 115200) { ReadTimeout = 100 };
                    
                    // CRITICAL FOR ESP32-S3 NATIVE USB: 
                    // Hardware DTR and RTS lines must be active, otherwise the board will not open its input buffer.
                    _serialPort.DtrEnable = true; 
                    _serialPort.RtsEnable = true;
                    
                    _serialPort.DataReceived += OnSerialDataReceived;
                    _serialPort.Open();

                    ConnectButton.Content = "Відключити";
                    ConnectButton.Background = SolidColorBrush.Parse("#FF5252");
                    ConnectionStatusText.Text = $"Статус: Підключено ({portName})";
                    ConnectionStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
                    SyncButton.IsEnabled = true;
                    SyncStatusText.Text = "Готово до синхронізації";
                }
                catch (Exception ex)
                {
                    ConnectionStatusText.Text = $"Помилка: {ex.Message}";
                    ConnectionStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
                }
            }
        }
    }

    /// <summary>
    /// Background handler for incoming data from the microcontroller. 
    /// Runs on a separate thread!
    /// </summary>
    private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            while (_serialPort.BytesToRead > 0)
            {
                string data = _serialPort.ReadLine().Trim();
                
                if (data.StartsWith("EXECUTE:"))
                {
                    // Process request to launch a program/script from Lilka
                    string target = data.Substring(8).Trim();
                    Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                }
                else if (data.StartsWith("ACK_"))
                {
                    // If this is a synchronization protocol acknowledgment signal, 
                    // pass it to the TaskCompletionSource to unblock the asynchronous wait.
                    _ackTcs?.TrySetResult(data);
                }
                else if (data.Length > 0)
                {
                    // Helper output for debugging text messages from the firmware
                    Console.WriteLine($"[ESP32] {data}");
                }
            }
        }
        catch (TimeoutException) { /* Read timeout is normal, just ignore it */ }
        catch (Exception ex)
        {
            Console.WriteLine($"Serial Error: {ex.Message}");
        }
    }

    // --- SYNCHRONIZATION LOGIC (PING-PONG PROTOCOL) ---

    /// <summary>
    /// Main synchronization state machine. Generates JSON, collects files, and manages the sending process.
    /// </summary>
    private async void OnSyncButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            SyncButton.IsEnabled = false;
            SyncProgressBar.Value = 0;
            SyncStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
            
            // Format the selected Color Picker value into a standard HEX string (#RRGGBB)
            string hexColor = $"#{ActiveColorPicker.Color.R:X2}{ActiveColorPicker.Color.G:X2}{ActiveColorPicker.Color.B:X2}";

            // 1. Build the configuration model for JSON serialization
            var output = new OutputConfig 
            { 
                ProfileName = ProfileNameTextBox.Text ?? "Profile",
                ActiveColor = hexColor
            };
            
            var filesToSend = new Dictionary<string, string>(); // Dictionary: [Filename on SD] -> [Local path on PC]

            foreach (var kvp in _deckConfigs)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value.IconPath) || !string.IsNullOrWhiteSpace(kvp.Value.Actions))
                {
                    var actionList = new List<string>();
                    if (kvp.Value.ActionType == "shortcut")
                        actionList = kvp.Value.Actions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    else if (kvp.Value.ActionType == "launch" && !string.IsNullOrWhiteSpace(kvp.Value.Actions))
                        actionList.Add(kvp.Value.Actions.Trim());

                    output.Buttons[kvp.Key] = new OutputButton
                    {
                        Icon = kvp.Value.IconPath,
                        Type = kvp.Value.ActionType,
                        Action = actionList
                    };

                    // Collect the list of images that need to be sent
                    if (!string.IsNullOrWhiteSpace(kvp.Value.IconFullPath) && File.Exists(kvp.Value.IconFullPath))
                    {
                        filesToSend[kvp.Value.IconPath] = kvp.Value.IconFullPath;
                    }
                }
            }

            string jsonString = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

            int profileId = (int)(ProfileIdSpinner.Value ?? 0);
            int totalFiles = 1 + filesToSend.Count;
            int currentFile = 0;

            // 2. Initialize connection (create profile folder on SD card)
            SyncStatusText.Text = $"Запуск (Профіль {profileId})...";
            _serialPort.WriteLine($"SYNC_START:{profileId}");
            if (!await WaitForAck("ACK_SYNC", 3000)) throw new Exception("Лілка не відповіла на SYNC_START");

            // 3. Send the configuration text file
            SyncStatusText.Text = "Відправка config.json...";
            await SendFileToSerial("config.json", jsonBytes);
            currentFile++;
            SyncProgressBar.Value = (currentFile * 100) / totalFiles;

            // 4. Send all binary .raw images
            foreach (var file in filesToSend)
            {
                SyncStatusText.Text = $"Відправка {file.Key}...";
                byte[] imgBytes = await File.ReadAllBytesAsync(file.Value);
                await SendFileToSerial(file.Key, imgBytes);
                
                currentFile++;
                SyncProgressBar.Value = (currentFile * 100) / totalFiles;
            }

            // 5. Finish transmission and command Lilka to reload its UI
            SyncStatusText.Text = "Перезавантаження UI Лілки...";
            _serialPort.WriteLine("SYNC_END");
            if (!await WaitForAck("ACK_END", 3000)) throw new Exception("Лілка не відповіла на SYNC_END");

            SyncStatusText.Text = "Синхронізація успішна!";
        }
        catch (Exception ex)
        {
            SyncStatusText.Text = $"Помилка: {ex.Message}";
            SyncStatusText.Foreground = SolidColorBrush.Parse("#FF5252");
        }
        finally
        {
            SyncButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Asynchronously waits for a specific text response from the microcontroller with a timeout.
    /// </summary>
    private async Task<bool> WaitForAck(string expectedAck, int timeoutMs)
    {
        _ackTcs = new TaskCompletionSource<string>();
        var timeoutTask = Task.Delay(timeoutMs);
        
        // Wait until Lilka sends an ACK, or until the timeout is reached
        var completedTask = await Task.WhenAny(_ackTcs.Task, timeoutTask);
        
        if (completedTask == timeoutTask) return false;
        return await _ackTcs.Task == expectedAck;
    }

    /// <summary>
    /// Sends a byte array to the microcontroller using a strict flow control protocol (Ping-Pong).
    /// </summary>
    private async Task SendFileToSerial(string fileName, byte[] data)
    {
        // Notify Lilka about the name and size of the incoming file
        _serialPort!.WriteLine($"FILE_START:{fileName}:{data.Length}");
        if (!await WaitForAck("ACK_FILE", 3000)) throw new Exception($"Немає ACK_FILE для {fileName}");

        int offset = 0;
        int chunkSize = 256; // Buffer size must strictly match the buffer on the ESP32 side

        while (offset < data.Length)
        {
            int size = Math.Min(chunkSize, data.Length - offset);
            
            // Send one block of bytes
            _serialPort.Write(data, offset, size);
            offset += size;

            // PING-PONG Protocol: The PC does not send the next chunk until Lilka confirms 
            // (ACK_CHUNK) that the previous chunk was successfully saved to the SD card.
            if (offset < data.Length)
            {
                if (!await WaitForAck("ACK_CHUNK", 5000))
                    throw new Exception($"Лілка зависла на записі {fileName} (offset: {offset})");
            }
        }

        // Wait for the final confirmation of file closure on the SD card side
        if (!await WaitForAck("ACK_DONE", 8000)) throw new Exception($"Немає ACK_DONE для {fileName}");
    }

    // --- UI LOGIC ---

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

            // Temporarily unsubscribe from the event to avoid false triggers during programmatic value changes
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

    /// <summary>
    /// Opens the image selection dialog. Converts the selected file to .raw (RGB565) format for the Lilka display.
    /// </summary>
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
                new FilePickerFileType("Зображення") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.raw", "*.rgb565" } }
            }
        });

        if (files.Count >= 1)
        {
            string inputPath = files[0].Path.LocalPath;
            string fileName = files[0].Name;
            string rawOutputPath = inputPath;

            // If a standard image is selected, convert it to 16-bit RAW (RGB565)
            if (!fileName.EndsWith(".raw") && !fileName.EndsWith(".rgb565"))
            {
                string rawFileName = Path.GetFileNameWithoutExtension(fileName) + ".raw";
                rawOutputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, rawFileName);
                
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
            _deckConfigs[_currentSelectedPosition].IconFullPath = rawOutputPath; 
        }
    }

    /// <summary>
    /// Local save of the JSON file. Serves for profile backups on the PC itself.
    /// </summary>
    private async void OnGenerateJsonClicked(object? sender, RoutedEventArgs e)
    {
        // Format the selected Color Picker value into a standard HEX string (#RRGGBB)
        string hexColor = $"#{ActiveColorPicker.Color.R:X2}{ActiveColorPicker.Color.G:X2}{ActiveColorPicker.Color.B:X2}";

        var output = new OutputConfig 
        { 
            ProfileName = ProfileNameTextBox.Text ?? "Profile",
            ActiveColor = hexColor
        };
        
        foreach (var kvp in _deckConfigs)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value.IconPath) || !string.IsNullOrWhiteSpace(kvp.Value.Actions))
            {
                var actionList = new List<string>();
                if (kvp.Value.ActionType == "shortcut")
                    actionList = kvp.Value.Actions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                else if (kvp.Value.ActionType == "launch" && !string.IsNullOrWhiteSpace(kvp.Value.Actions))
                    actionList.Add(kvp.Value.Actions.Trim());

                output.Buttons[kvp.Key] = new OutputButton
                {
                    Icon = kvp.Value.IconPath,
                    Type = kvp.Value.ActionType,
                    Action = actionList
                };
            }
        }

        string jsonString = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Зберегти конфігурацію",
                SuggestedFileName = "config.json",
                DefaultExtension = "json"
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

// --- STRUCTURES FOR JSON SERIALIZATION ---

/// <summary>
/// Root model of the JSON configuration that will be saved to the microcontroller's SD card.
/// </summary>
public class OutputConfig
{
    [JsonPropertyName("profileName")]
    public string ProfileName { get; set; } = "Profile";

    // Global active color property. Included in JSON serialization.
    [JsonPropertyName("activeColor")]
    public string ActiveColor { get; set; } = "#00FFFF"; 

    [JsonPropertyName("buttons")]
    public Dictionary<string, OutputButton> Buttons { get; set; } = new();
}

/// <summary>
/// Model of a single button for JSON.
/// </summary>
public class OutputButton
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "shortcut";

    [JsonPropertyName("action")]
    public List<string> Action { get; set; } = new();
}
