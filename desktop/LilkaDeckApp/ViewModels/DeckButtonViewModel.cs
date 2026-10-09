using System;
using System.Windows.Input;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Mvvm;

namespace LilkaDeckApp.ViewModels;

/// <summary>One virtual button of the deck: where it is, what it says without an icon, and which icon file it shows.</summary>
public sealed class DeckButtonViewModel : ObservableObject
{
    private string? _iconFilePath;
    private bool _isSelected;

    public DeckButtonViewModel(DeckPosition position, string defaultLabel, Action<DeckButtonViewModel> onSelected)
    {
        Position = position;
        DefaultLabel = defaultLabel;
        SelectCommand = new RelayCommand(() => onSelected(this));
    }

    public DeckPosition Position { get; }

    /// <summary>The text on the button while it has no icon.</summary>
    public string DefaultLabel { get; }

    public ICommand SelectCommand { get; }

    /// <summary>The cached .raw file to draw, or null when the button has no icon.</summary>
    public string? IconFilePath => _iconFilePath;

    public bool IsSelected
    {
        get => _isSelected;
        internal set => SetProperty(ref _isSelected, value);
    }

    internal void ShowIcon(string? iconFilePath)
    {
        _iconFilePath = string.IsNullOrWhiteSpace(iconFilePath) ? null : iconFilePath;

        // Raised even when the path is the same: an icon re-imported under the same name has new content.
        OnPropertyChanged(nameof(IconFilePath));
    }
}
