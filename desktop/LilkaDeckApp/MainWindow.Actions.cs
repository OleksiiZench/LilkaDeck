using Avalonia.Threading;
using LilkaDeckApp.Services;

namespace LilkaDeckApp;

// What a deck button does: recording a keyboard shortcut in the editor, and launching programs the device asks for.
public partial class MainWindow
{
    private LaunchHandler? _launchHandler;

    private LaunchHandler Launcher => _launchHandler ??= new LaunchHandler(new ShellTargetLauncher());

    private void OnActionsTextBoxKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSelectedPosition)) return;
        if (_profileData.GetConfig(_currentSelectedPosition).ActionType != "shortcut") return;

        // In shortcut mode the box is a recorder: nothing typed may reach the text itself.
        e.Handled = true;

        if (e.Key == Avalonia.Input.Key.Back || e.Key == Avalonia.Input.Key.Delete)
        {
            SetShortcutText("", "");
            return;
        }

        switch (ShortcutCapture.TryCapture(e.Key, e.KeyModifiers, out var shortcut))
        {
            case CaptureOutcome.Captured:
                SetShortcutText(shortcut!.ToDisplayText(), shortcut.ToStoredText());
                break;
            case CaptureOutcome.Unsupported:
                AppLog($"Клавіша {e.Key} поки не підтримується прошивкою.", true);
                break;
        }
    }

    private void SetShortcutText(string displayText, string storedText)
    {
        ActionsTextBox.Text = displayText;
        _profileData.UpdateConfig(_currentSelectedPosition, c => c.Actions = storedText);
        TriggerAutoSync();
    }

    private void HandleExecuteRequest(string target)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!Launcher.TryLaunch(target, out string? error))
            {
                AppLog($"Launch error: {error}", true);
            }
        });
    }
}
