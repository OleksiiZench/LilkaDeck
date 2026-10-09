namespace LilkaDeckApp.Sync;

/// <summary>The profile that is open on the profile screen, as far as saving is concerned.</summary>
public interface IOpenProfile
{
    int? CurrentProfileId { get; }
    bool HasProfiles { get; }
    bool IsLoading { get; }
    string Name { get; }
    
    /// <summary>"#RRGGBB".</summary>
    string ActiveColor { get; }
}
