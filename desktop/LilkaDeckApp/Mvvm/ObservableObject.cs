using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LilkaDeckApp.Mvvm;

/// <summary>
/// Base class for anything the UI binds to. The member names match CommunityToolkit.Mvvm,
/// so switching to that package later is a change of namespace, not of design.
/// View models are only touched on the UI thread.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
