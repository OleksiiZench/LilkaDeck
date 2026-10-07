using System;
using System.Threading.Tasks;
using LilkaDeckApp.Sync;

namespace LilkaDeckApp;

// Saving the editor to the device: changes are collected for a moment and sent in one go.
public partial class MainWindow
{
    private static readonly TimeSpan AutoSyncDelay = TimeSpan.FromSeconds(1.5);

    private AutoSyncCoordinator? _autoSync;

    private AutoSyncCoordinator AutoSync => _autoSync ??= new AutoSyncCoordinator(SyncOnceAsync, HandleError, AutoSyncDelay);

    private void TriggerAutoSync()
    {
        if (_viewModel.Profile.IsLoading || !_comService.IsConnected) return;

        AutoSync.RequestSync();
    }

    private async Task SyncOnceAsync()
    {
        var profile = _viewModel.Profile;
        if (!_comService.IsConnected || !profile.HasProfiles) return;

        _viewModel.Activity.Progress = 0;
        AppLog("Автосинхронізація...");

        var payload = _profileData.BuildSyncPayload(profile.Name, profile.ActiveColor);
        var progress = new Progress<int>(percent => _viewModel.Activity.Progress = percent);
        var status = new Progress<string>(message => AppLog(message));

        await _comService.SyncDataAsync(profile.CurrentProfileId ?? 0, payload.jsonBytes, payload.filesToSend, progress, status);
        AppLog("Збережено на пристрій!");

        _profileData.MarkUploaded(payload.filesToSend);
    }
}
