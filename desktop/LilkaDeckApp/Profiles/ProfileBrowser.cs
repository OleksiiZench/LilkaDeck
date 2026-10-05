using System;
using System.Collections.Generic;
using System.Linq;

namespace LilkaDeckApp.Profiles;

/// <summary>The profiles that exist on the device and which one is being looked at.</summary>
public sealed class ProfileBrowser
{
    private List<int> _ids = new();
    private int _index;

    public bool HasProfiles => _ids.Count > 0;
    public int? CurrentId => HasProfiles ? _ids[_index] : null;

    // The device refuses to delete its last profile.
    public bool CanDelete => _ids.Count > 1;

    /// <summary>Selects the preferred profile, or the first one when it does not exist.</summary>
    public void SetProfiles(IEnumerable<int> ids, int preferredId)
    {
        _ids = ids.Distinct().OrderBy(id => id).ToList();
        _index = Math.Max(0, _ids.IndexOf(preferredId));
    }

    public int? MoveNext()
    {
        if (!HasProfiles) return null;

        _index = (_index + 1) % _ids.Count;
        return _ids[_index];
    }

    public int? MovePrevious()
    {
        if (!HasProfiles) return null;

        _index = (_index == 0 ? _ids.Count : _index) - 1;
        return _ids[_index];
    }
}
