using System.Globalization;
using System.IO;

namespace LilkaDeckApp.Profiles;

/// <summary>One icon cache per profile, so icons with the same name in different profiles never mix.</summary>
public sealed class ProfileCaches
{
    private readonly string _rootDirectory;

    public ProfileCaches(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public IconCache For(int profileId) =>
        new(Path.Combine(_rootDirectory, "profile_" + profileId.ToString(CultureInfo.InvariantCulture)));
}
