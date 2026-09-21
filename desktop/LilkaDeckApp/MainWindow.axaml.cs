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

// Simple model for storing each button's configuration
public class ButtonConfig
{
    public string IconPath { get; set; } = "";
    public string Actions { get; set; } = "";
}

public partial class MainWindow : Window
{
    // Dictionary storing the state of all 8 buttons
    private readonly Dictionary<string, ButtonConfig> _deckConfigs = new();

    // Variable tracking the currently selected button for editing
    private string _currentSelectedPosition = "";

    public MainWindow()
    {
        InitializeComponent();

        // Initialize the dictionary with empty configurations for each position
        string[] positions = { "LeftUp", "LeftLeft", "LeftRight", "LeftDown", "RightUp", "RightLeft", "RightRight", "RightDown" };
        foreach (var pos in positions)
        {
            _deckConfigs[pos] = new ButtonConfig();
        }
    }

    // Updated button selection handler
    private void OnDeckButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string position)
        {
            _currentSelectedPosition = position;
            
            // Enable the button settings panel
            ButtonSettingsPanel.IsEnabled = true;
            SelectedButtonLabel.Text = $"Редагування: {position}";

            var config = _deckConfigs[position];
            IconPathTextBox.Text = config.IconPath;
            ActionsTextBox.Text = config.Actions;
        }
    }

    // Handler for text changes in the macros field
    private void OnActionsTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentSelectedPosition) && sender is TextBox tb)
        {
            _deckConfigs[_currentSelectedPosition].Actions = tb.Text ?? "";
        }
    }

    // Handler for image selection with automatic conversion
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
                // Allow selection of standard image formats
                new FilePickerFileType("Зображення (PNG, JPG)") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } },
                new FilePickerFileType("Готові RAW") { Patterns = new[] { "*.raw", "*.rgb565" } }
            }
        });

        if (files.Count >= 1)
        {
            // Get the absolute path to the selected file
            string inputPath = files[0].Path.LocalPath;
            string fileName = files[0].Name;

            // If the user selected a PNG or JPG, convert it immediately
            if (!fileName.EndsWith(".raw") && !fileName.EndsWith(".rgb565"))
            {
                // Generate the new file name (e.g., discord.png -> discord.raw)
                string rawFileName = Path.GetFileNameWithoutExtension(fileName) + ".raw";
                
                // Save the RAW file in the same directory as the original image
                string rawOutputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, rawFileName);
                
                try
                {
                    ImageConverter.ConvertToRgb565Raw(inputPath, rawOutputPath);
                    
                    // Update the file name to be written to the JSON config
                    fileName = rawFileName;
                }
                catch (Exception ex)
                {
                    // Log conversion errors to the console
                    Console.WriteLine($"Помилка конвертації: {ex.Message}");
                }
            }

            // Update UI and model (file name will now always have a .raw extension)
            IconPathTextBox.Text = fileName;
            _deckConfigs[_currentSelectedPosition].IconPath = fileName;
        }
    }

    // JSON GENERATION AND SAVING IMPLEMENTATION
    private async void OnGenerateJsonClicked(object? sender, RoutedEventArgs e)
    {
        var output = new OutputConfig
        {
            ProfileName = ProfileNameTextBox.Text ?? "Profile"
        };

        // Filter and process only buttons that have configured settings
        foreach (var kvp in _deckConfigs)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value.IconPath) || !string.IsNullOrWhiteSpace(kvp.Value.Actions))
            {
                // Split the string "CTRL, ALT, T" into an array ["CTRL", "ALT", "T"]
                var actionList = kvp.Value.Actions
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                output.Buttons[kvp.Key] = new OutputButton
                {
                    Icon = kvp.Value.IconPath,
                    Action = actionList
                };
            }
        }

        // Configure JSON serialization with indentation
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(output, options);

        // Invoke the native system file save dialog
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
                // Write the generated JSON string to the file
                await using var stream = await file.OpenWriteAsync();
                using var streamWriter = new StreamWriter(stream);
                await streamWriter.WriteAsync(jsonString);
            }
        }
    }
}

// Models for generating the final JSON structure
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

    [JsonPropertyName("action")]
    public List<string> Action { get; set; } = new();
}
