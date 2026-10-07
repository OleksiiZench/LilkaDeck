using System;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.ViewModels;

namespace LilkaDeckApp;

// The window's link to its view model: the log, the connection status and the profile screen.
public partial class MainWindow
{
    private MainViewModel _viewModel = null!;

    // Call once from the constructor, after _comService and _profileData have been created.
    private void InitializeShell()
    {
        var activity = new ActivityViewModel();
        var connection = new ConnectionViewModel();
        var profile = new ProfileViewModel(
            _comService, new ProfileLoader(_comService, _profileData.Caches), activity, connection, HandleError);
        _viewModel = new MainViewModel(activity, connection, profile);

        profile.NameEdited += TriggerAutoSync;
        profile.ColorEdited += hex =>
        {
            _comService.SendColorPreview(hex);
            TriggerAutoSync();
        };
        profile.Loaded += OnProfileLoaded;
        profile.Removed += () => _profileData.ClearState();

        DataContext = _viewModel;

        // Keeping the newest line in view is a matter for the view, not for the view model.
        activity.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActivityViewModel.LogText))
            {
                Dispatcher.UIThread.Post(() => LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0);
            }
        };
    }

    private void OnProfileLoaded(int profileId, ProfileDocument? document)
    {
        if (document != null)
        {
            _profileData.ShowProfile(profileId, document);
            UpdateDeckVisuals();
        }

        if (!string.IsNullOrEmpty(_currentSelectedPosition))
        {
            OnDeckButtonClicked(new Avalonia.Controls.Button { Tag = _currentSelectedPosition }, new RoutedEventArgs());
        }
    }

    // Safe to call from any thread: the log is only touched on the UI thread.
    private void AppLog(string message, bool isError = false) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (isError) _viewModel.Activity.Error(message);
            else _viewModel.Activity.Info(message);
        });

    private async void HandleConnected(string portName)
    {
        Dispatcher.UIThread.Post(() => _viewModel.Connection.SetConnected(portName));
        AppLog($"Підключено до порту {portName}");
        AppLog("Завантаження конфігурації з Лілки...");

        await Dispatcher.UIThread.InvokeAsync(() => _viewModel.Profile.RefreshAsync());
    }

    private void HandleDisconnected()
    {
        Dispatcher.UIThread.Post(() => _viewModel.Connection.SetSearching());
        AppLog("Пристрій відключено. Пошук...", true);
    }

    private void HandleLogMessage(string message) => AppLog(message);

    private void HandleError(Exception ex)
    {
        Dispatcher.UIThread.Post(() => _viewModel.Connection.SetError(ex.Message));
        AppLog($"Синхронізацію перервано: {ex.Message}", true);
    }
}
