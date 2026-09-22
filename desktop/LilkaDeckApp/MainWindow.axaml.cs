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

public class ButtonConfig
{
    public string IconPath { get; set; } = "";
    public string IconFullPath { get; set; } = ""; // NEW: Зберігаємо повний шлях на ПК для синхронізації
    public string ActionType { get; set; } = "shortcut";
    public string Actions { get; set; } = "";
}

public partial class MainWindow : Window
{
    private readonly Dictionary<string, ButtonConfig> _deckConfigs = new();
    private string _currentSelectedPosition = "";
    private SerialPort? _serialPort;
    
    // NEW: Допоміжний об'єкт для асинхронного очікування відповідей (ACK) від Лілки
    private TaskCompletionSource<string>? _ackTcs;

    public MainWindow()
    {
        InitializeComponent();

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
        if (_serialPort != null && _serialPort.IsOpen)
        {
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
            if (ComPortComboBox.SelectedItem is string portName)
            {
                try
                {
                    _serialPort = new SerialPort(portName, 115200) { ReadTimeout = 100 };
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

    // Змінений обробник Serial для підтримки протоколу ACK
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
                    string target = data.Substring(8).Trim();
                    Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                }
                else if (data.StartsWith("ACK_"))
                {
                    // Передаємо отриманий ACK у машину станів синхронізації
                    _ackTcs?.TrySetResult(data);
                }
                else if (data.Length > 0)
                {
                    Console.WriteLine($"[ESP32] {data}");
                }
            }
        }
        catch (TimeoutException) { /* Нормально при повільному читанні */ }
        catch (Exception ex)
        {
            Console.WriteLine($"Serial Error: {ex.Message}");
        }
    }

    // --- НОВА ЛОГІКА СИНХРОНІЗАЦІЇ ---

    private async void OnSyncButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            SyncButton.IsEnabled = false;
            SyncProgressBar.Value = 0;
            SyncStatusText.Foreground = SolidColorBrush.Parse("#4CAF50");
            
            // 1. Збираємо конфігурацію та списки файлів
            var output = new OutputConfig { ProfileName = ProfileNameTextBox.Text ?? "Profile" };
            var filesToSend = new Dictionary<string, string>(); // Назва файлу -> Повний шлях на ПК

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

            // 2. Ініціалізуємо старт синхронізації
            SyncStatusText.Text = $"Запуск (Профіль {profileId})...";
            _serialPort.WriteLine($"SYNC_START:{profileId}");
            if (!await WaitForAck("ACK_SYNC", 3000)) throw new Exception("Лілка не відповіла на SYNC_START");

            // 3. Відправляємо config.json
            SyncStatusText.Text = "Відправка config.json...";
            await SendFileToSerial("config.json", jsonBytes);
            currentFile++;
            SyncProgressBar.Value = (currentFile * 100) / totalFiles;

            // 4. Відправляємо всі картинки .raw
            foreach (var file in filesToSend)
            {
                SyncStatusText.Text = $"Відправка {file.Key}...";
                byte[] imgBytes = await File.ReadAllBytesAsync(file.Value);
                await SendFileToSerial(file.Key, imgBytes);
                
                currentFile++;
                SyncProgressBar.Value = (currentFile * 100) / totalFiles;
            }

            // 5. Завершуємо синхронізацію
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

    private async Task<bool> WaitForAck(string expectedAck, int timeoutMs)
    {
        _ackTcs = new TaskCompletionSource<string>();
        var timeoutTask = Task.Delay(timeoutMs);
        var completedTask = await Task.WhenAny(_ackTcs.Task, timeoutTask);
        
        if (completedTask == timeoutTask) return false;
        return await _ackTcs.Task == expectedAck;
    }

    private async Task SendFileToSerial(string fileName, byte[] data)
    {
        _serialPort!.WriteLine($"FILE_START:{fileName}:{data.Length}");
        if (!await WaitForAck("ACK_FILE", 3000)) throw new Exception($"Немає ACK_FILE для {fileName}");

        int offset = 0;
        int chunkSize = 256;
        while (offset < data.Length)
        {
            int size = Math.Min(chunkSize, data.Length - offset);
            _serialPort.Write(data, offset, size);
            offset += size;
            
            // Критично важлива затримка, щоб SD-карта на Лілці встигла записати дані
            await Task.Delay(50); 
        }

        if (!await WaitForAck("ACK_DONE", 8000)) throw new Exception($"Немає ACK_DONE для {fileName}");
    }

    // --- СТАРА ЛОГІКА ІНТЕРФЕЙСУ ---

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
                new FilePickerFileType("Зображення") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.raw", "*.rgb565" } }
            }
        });

        if (files.Count >= 1)
        {
            string inputPath = files[0].Path.LocalPath;
            string fileName = files[0].Name;
            string rawOutputPath = inputPath;

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
            // NEW: Зберігаємо повний шлях для синхронізації по кабелю
            _deckConfigs[_currentSelectedPosition].IconFullPath = rawOutputPath; 
        }
    }

    // Локальне збереження залишено як резервний варіант
    private async void OnGenerateJsonClicked(object? sender, RoutedEventArgs e)
    {
        // ... (Код OnGenerateJsonClicked залишився без змін, як у твоїй версії) ...
        var output = new OutputConfig { ProfileName = ProfileNameTextBox.Text ?? "Profile" };
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
