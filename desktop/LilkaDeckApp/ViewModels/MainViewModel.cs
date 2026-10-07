using LilkaDeckApp.Mvvm;

namespace LilkaDeckApp.ViewModels;

/// <summary>Everything the main window shows. It grows one region at a time as the window moves to MVVM.</summary>
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel(ActivityViewModel activity, ConnectionViewModel connection, ProfileViewModel profile)
    {
        Activity = activity;
        Connection = connection;
        Profile = profile;
    }

    public ActivityViewModel Activity { get; }
    public ConnectionViewModel Connection { get; }
    public ProfileViewModel Profile { get; }
}
