using Avalonia.Threading;
using LilkaDeckApp.Services;
using Avalonia.Input;

namespace LilkaDeckApp;

// What a deck button does: recording a keyboard shortcut in the editor, and launching programs the device asks for.
public partial class MainWindow
{
    private LaunchHandler? _launchHandler;

    private LaunchHandler AppLauncher => _launchHandler ??= new LaunchHandler(new ShellTargetLauncher());

    private void OnActionsTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        var editor = _viewModel.Editor;
        if (!editor.IsShortcutMode) return;

        // In shortcut mode the box is a recorder: nothing typed may reach the text itself.
        e.Handled = true;

        if (e.Key is Key.Back or Key.Delete)
        {
            editor.ClearAction();
            return;
        }

        switch (ShortcutCapture.TryCapture(e.Key, e.KeyModifiers, out var shortcut))
        {
            case CaptureOutcome.Captured:
                editor.SetShortcut(shortcut!);
                break;
            case CaptureOutcome.Unsupported:
                AppLog($"Клавіша {e.Key} поки не підтримується прошивкою.", true);
                break;
        }
    }

    private void HandleExecuteRequest(string target)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!AppLauncher.TryLaunch(target, out string? error))
            {
                AppLog($"Launch error: {error}", true);
            }
        });
    }
}
