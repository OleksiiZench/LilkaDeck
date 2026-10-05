using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LilkaDeckApp.Domain;

namespace LilkaDeckApp.Profiles;

/// <summary>
/// Reads a profile from the device: its config.json and the icons it uses, which are saved
/// in that profile's cache. The device is the source of truth, so icons are always fetched again.
/// </summary>
public sealed class ProfileLoader
{
    private const string ConfigFileName = "config.json";

    private readonly IProfileFileSource _source;
    private readonly ProfileCaches _caches;

    public ProfileLoader(IProfileFileSource source, ProfileCaches caches)
    {
        _source = source;
        _caches = caches;
    }

    /// <summary>
    /// Returns null when the config could not be read. <paramref name="onConfigLoaded"/> fires as soon as
    /// the config is known, before the icons are fetched, so the UI can show the name and color early.
    /// </summary>
    public async Task<ProfileDocument?> LoadAsync(
        int profileId,
        Action<ProfileDocument>? onConfigLoaded = null,
        Action<string>? onIconDownloading = null,
        CancellationToken cancellationToken = default)
    {
        byte[]? configBytes = await _source.DownloadFileAsync(profileId, ConfigFileName, cancellationToken);
        if (configBytes == null) return null;

        if (!ProfileSerializer.TryDeserialize(Encoding.UTF8.GetString(configBytes), out var document, out _)) return null;

        onConfigLoaded?.Invoke(document);
        await DownloadIconsAsync(profileId, document, onIconDownloading, cancellationToken);
        return document;
    }

    // Names that would point outside the cache folder are ignored, whatever the device says.
    private async Task DownloadIconsAsync(
        int profileId, ProfileDocument document, Action<string>? onIconDownloading, CancellationToken cancellationToken)
    {
        var cache = _caches.For(profileId);
        var iconNames = document.Buttons.Values
            .Select(button => button.IconFileName)
            .Where(IconCache.IsSafeFileName)
            .Distinct();

        foreach (string iconName in iconNames)
        {
            onIconDownloading?.Invoke(iconName);

            byte[]? content = await _source.DownloadFileAsync(profileId, iconName, cancellationToken);
            if (content != null) cache.Save(iconName, content);
        }
    }
}
