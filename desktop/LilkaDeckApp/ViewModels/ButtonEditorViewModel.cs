using System;
using System.Collections.Generic;
using System.Windows.Input;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Mvvm;
using LilkaDeckApp.Profiles;

namespace LilkaDeckApp.ViewModels;

public sealed record ActionTypeOption(ActionType Type, string Title);

/// <summary>
/// The settings of the selected deck button: its icon, the type of its action and the action text.
/// Showing a button is not an edit: only what the user changes reaches the store and raises <see cref="Edited"/>.
/// </summary>
public sealed class ButtonEditorViewModel : ObservableObject
{
    private static readonly ActionTypeOption ShortcutOption = new(ActionType.Shortcut, "Гарячі клавіші (Shortcut)");
    private static readonly ActionTypeOption LaunchOption = new(ActionType.Launch, "Запуск програми / URL (Launch)");

    private readonly IButtonStore _store;
    private readonly Func<bool> _isProfileLoading;
    private DeckPosition? _position;
    private ActionTypeOption? _selectedActionType = ShortcutOption;
    private string _iconFileName = "";
    private string _actionText = "";
    private bool _isShowing;

    public ButtonEditorViewModel(IButtonStore store, Func<bool> isProfileLoading)
    {
        _store = store;
        _isProfileLoading = isProfileLoading;
        MediaCommand = new RelayCommand<string>(SetMedia);
    }

    /// <summary>Raised when the user changed something that has to be saved.</summary>
    public event Action? Edited;

    public IReadOnlyList<ActionTypeOption> ActionTypeOptions { get; } = new[] { ShortcutOption, LaunchOption };

    /// <summary>Sets the action text to the media key named by the command parameter, for example "MEDIA_NEXT".</summary>
    public ICommand MediaCommand { get; }

    public bool HasSelection => _position.HasValue;

    public string Header => _position is { } position ? $"Редагування: {position.ToWireName()}" : "Оберіть кнопку...";

    /// <summary>True when the action text is a recorded key combination rather than typed text.</summary>
    public bool IsShortcutMode => HasSelection && IsShortcut;

    public string IconFileName
    {
        get => _iconFileName;
        private set => SetProperty(ref _iconFileName, value);
    }

    public ActionTypeOption? SelectedActionType
    {
        get => _selectedActionType;
        set
        {
            if (value == null || !SetProperty(ref _selectedActionType, value)) return;

            RaiseModeChanged();
            if (_isShowing || _position is not { } position) return;

            _store.SetActionType(position, value.Type);
            Edited?.Invoke();
        }
    }

    public string ActionText
    {
        get => _actionText;
        set
        {
            string text = value ?? "";
            if (!SetProperty(ref _actionText, text) || _isShowing) return;
            if (_position is not { } position || _isProfileLoading()) return;

            _store.SetActionText(position, text);
            Edited?.Invoke();
        }
    }

    public string ActionHint => IsShortcut ? "Комбінація клавіш або медіа-дія:" : "Шлях до програми або URL:";

    public string ActionPlaceholder =>
    IsShortcut ? "Клікніть і натисніть комбінацію..." : @"C:\Apps\Discord.exe або https://youtube.com";

    public bool IsActionReadOnly => IsShortcut;

    public bool AreMediaButtonsVisible => IsShortcut;

    private bool IsShortcut => _selectedActionType?.Type != ActionType.Launch;

    /// <summary>Shows the settings of a deck button in the editor.</summary>
    public void Show(DeckPosition position)
    {
        var button = _store.GetButton(position);

        WhileShowing(() =>
        {
            _position = position;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(Header));

            IconFileName = button.IconFileName;
            SelectedActionType = button.ActionType == ActionType.Launch ? LaunchOption : ShortcutOption;
            ActionText = DisplayText(button.ActionType, button.ActionText);
        });
    }

    /// <summary>Shows the name of an icon that was just imported; the import itself saves it.</summary>
    public void ShowImportedIcon(string iconFileName) => IconFileName = iconFileName;

    public void SetShortcut(Shortcut shortcut) => ApplyAction(shortcut.ToDisplayText(), shortcut.ToStoredText());

    public void ClearAction() => ApplyAction("", "");

    private void SetMedia(string mediaToken) => ApplyAction(mediaToken, mediaToken);

    // The box shows what was set; the store gets the text in its stored form.
    private void ApplyAction(string displayText, string storedText)
    {
        if (_position is not { } position) return;

        WhileShowing(() => ActionText = displayText);
        _store.SetActionText(position, storedText);
        Edited?.Invoke();
    }

    // A shortcut is stored as "CTRL, T" and shown as "CTRL + T"; a launch target is shown as it is.
    private static string DisplayText(ActionType type, string storedText) =>
    type == ActionType.Shortcut ? storedText.Replace(", ", " + ") : storedText;

    private void WhileShowing(Action change)
    {
        bool wasShowing = _isShowing;
        _isShowing = true;
        try
        {
            change();
        }
        finally
        {
            _isShowing = wasShowing;
        }
    }

    private void RaiseModeChanged()
    {
        OnPropertyChanged(nameof(IsShortcutMode));
        OnPropertyChanged(nameof(ActionHint));
        OnPropertyChanged(nameof(ActionPlaceholder));
        OnPropertyChanged(nameof(IsActionReadOnly));
        OnPropertyChanged(nameof(AreMediaButtonsVisible));
    }
}
