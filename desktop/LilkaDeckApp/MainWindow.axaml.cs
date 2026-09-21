using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace LilkaDeckApp;

// Model for internal UI state management
public class ButtonConfig
{
    public string IconPath { get; set; } = "";
    public string ActionType { get; set; } = "shortcut"; // Defaults to standard macro
    public string Actions { get; set; } = "";
}

public partial class MainWindow : Window
{
    private readonly Dictionary<string, ButtonConfig> _deckConfigs = new();
    private string _currentSelectedPosition = "";

    public MainWindow()
    {
        InitializeComponent();

        string[] positions = { "LeftUp", "LeftLeft", "LeftRight", "LeftDown", "RightUp", "RightLeft", "RightRight", "RightDown" };
        foreach (var pos in positions)
        {
            _deckConfigs[pos] = new ButtonConfig();
        }
    }

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

            // Unsubscribe temporarily to prevent overwriting the model during UI update
            ActionTypeComboBox.SelectionChanged -= OnActionTypeChanged;
            
            // Set the correct combobox item based on the current configuration
            ActionTypeComboBox.SelectedIndex = config.ActionType == "launch" ? 1 : 0;
            UpdateActionHint(config.ActionType);
            
            ActionTypeComboBox.SelectionChanged += OnActionTypeChanged;
        }
    }

    // Handles the change between 'Shortcut' and 'Launch' types
    private void OnActionTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;

        if (ActionTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string type)
        {
            _deckConfigs[_currentSelectedPosition].ActionType = type;
            UpdateActionHint(type);
        }
    }

    // Dynamically updates the UI text based on the selected action type
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
                    Console.WriteLine($"Помилка конвертації: {ex.Message}");
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

                // Process shortcuts as comma-separated arrays, and launches as a single array item
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
                    Type = kvp.Value.ActionType, // NEW FIELD
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

// Serialization models
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

    // Added serialization for action type routing
    [JsonPropertyName("type")]
    public string Type { get; set; } = "shortcut";

    [JsonPropertyName("action")]
    public List<string> Action { get; set; } = new();
}
