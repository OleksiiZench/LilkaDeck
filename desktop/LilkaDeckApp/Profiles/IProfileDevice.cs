using System.Collections.Generic;
using System.Threading.Tasks;

namespace LilkaDeckApp.Profiles;

/// <summary>What the profile screen needs from the device.</summary>
public interface IProfileDevice
{
    Task<IReadOnlyList<int>> GetProfileIdsAsync();

    /// <summary>Returns the id of the new profile, or null when the device did not answer.</summary>
    Task<int?> CreateProfileAsync();

    /// <summary>Returns false when the device refuses or does not answer.</summary>
    Task<bool> DeleteProfileAsync(int profileId);
}
