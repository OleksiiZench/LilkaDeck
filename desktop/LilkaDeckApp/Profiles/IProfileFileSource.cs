using System.Threading;
using System.Threading.Tasks;

namespace LilkaDeckApp.Profiles;

/// <summary>Where the files of a profile are fetched from. The device implements it.</summary>
public interface IProfileFileSource
{
    /// <summary>Returns null when the file does not exist or the device did not answer.</summary>
    Task<byte[]?> DownloadFileAsync(int profileId, string fileName, CancellationToken cancellationToken);
}
