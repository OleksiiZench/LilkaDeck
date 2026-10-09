using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LilkaDeckApp.Device;
using LilkaDeckApp.Profiles;
using LilkaDeckApp.ViewModels;

namespace LilkaDeckApp.Sync;

/// <summary>
/// Saves the open profile to the device. <see cref="RequestSave"/> waits for a pause in the edits,
/// <see cref="SaveNowAsync"/> saves at once; the progress and the outcome go to the activity log.
/// Used on the UI thread, because it writes to view models.
/// </summary>
public sealed class ProfileSaver
{
    private const string ConfigFileName = "config.json";
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(1.5);
    
    private readonly IOpenProfile _profile;
    private readonly DeckEditSession _session;
    private readonly IProfileSyncDevice _device;
    private readonly ActivityViewModel _activity;
    private readonly AutoSyncCoordinator _coordinator;
    
    public ProfileSaver(
        IOpenProfile profile,
        DeckEditSession session,
        IProfileSyncDevice device,
        ActivityViewModel activity,
        Action<Exception> onError,
        TimeSpan? delay = null)
    {
        _profile = profile;
        _session = session;
        _device = device;
        _activity = activity;
        _coordinator = new AutoSyncCoordinator(SaveOnceAsync, onError, delay ?? DefaultDelay);
    }
    
    /// <summary>Asks for a save soon. Ignored while a profile is loading or the device is missing.</summary>
    public void RequestSave()
    {
        if (_profile.IsLoading || !_device.IsConnected) return;
        
        _coordinator.RequestSync();
    }
    
    public Task SaveNowAsync() => _coordinator.SyncNowAsync();
    
    private async Task SaveOnceAsync()
    {
        if (!_device.IsConnected || !_profile.HasProfiles) return;
        
        _activity.Progress = 0;
        _activity.Info("Автосинхронізація...");
        
        int profileId = _profile.CurrentProfileId ?? 0;
        var payload = _session.BuildSyncPayload(_profile.Name, _profile.ActiveColor);
        
        // Reading everything first means a missing file stops the save before anything is sent.
        var icons = new List<SyncFile>();
        foreach (var (name, path) in payload.IconFiles)
        {
            icons.Add(new SyncFile(name, await File.ReadAllBytesAsync(path)));
        }
        
        var progress = new Progress<SyncProgress>(value => ShowProgress(profileId, value));
        await _device.SyncAsync(profileId, new SyncFile(ConfigFileName, payload.ConfigJson), icons, progress);
        
        _activity.Info("Збережено на пристрій!");
        _session.MarkUploaded(payload.IconFiles);
    }
    
    private void ShowProgress(int profileId, SyncProgress progress)
    {
        switch (progress.Stage)
        {
            case SyncStage.Started: _activity.Info($"Запуск (Профіль {profileId})..."); break;
            case SyncStage.FileStarted: _activity.Info($"Відправка {progress.FileName}..."); break;
            case SyncStage.FileFinished: _activity.Progress = progress.Percent; break;
            case SyncStage.Finishing: _activity.Info("Перезавантаження UI Лілки..."); break;
        }
    }
}
