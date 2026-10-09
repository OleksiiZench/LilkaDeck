using System;
using System.Collections.Generic;
using System.Linq;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Mvvm;

namespace LilkaDeckApp.ViewModels;

/// <summary>The eight virtual buttons of the deck and which of them is selected for editing.</summary>
public sealed class DeckViewModel : ObservableObject
{
    private readonly Dictionary<DeckPosition, DeckButtonViewModel> _buttons;
    private DeckPosition? _selectedPosition;

    public DeckViewModel()
    {
        _buttons = DeckPositions.All.ToDictionary(
            position => position,
            position => new DeckButtonViewModel(position, DefaultLabelOf(position), button => Select(button.Position)));
    }

    /// <summary>Raised on every selection, also when the selected button is selected again.</summary>
    public event Action<DeckPosition>? Selected;

    public DeckPosition? SelectedPosition => _selectedPosition;

    // One property per position, so that the layout in XAML can bind to each of them.
    public DeckButtonViewModel LeftUp => Get(DeckPosition.LeftUp);
    public DeckButtonViewModel LeftLeft => Get(DeckPosition.LeftLeft);
    public DeckButtonViewModel LeftRight => Get(DeckPosition.LeftRight);
    public DeckButtonViewModel LeftDown => Get(DeckPosition.LeftDown);
    public DeckButtonViewModel RightUp => Get(DeckPosition.RightUp);
    public DeckButtonViewModel RightLeft => Get(DeckPosition.RightLeft);
    public DeckButtonViewModel RightRight => Get(DeckPosition.RightRight);
    public DeckButtonViewModel RightDown => Get(DeckPosition.RightDown);

    public IEnumerable<string> IconFilePaths => _buttons.Values.Select(button => button.IconFilePath).OfType<string>();

    public DeckButtonViewModel Get(DeckPosition position) => _buttons[position];

    public void Select(DeckPosition position)
    {
        if (_selectedPosition is DeckPosition previous) _buttons[previous].IsSelected = false;

        _selectedPosition = position;
        _buttons[position].IsSelected = true;
        OnPropertyChanged(nameof(SelectedPosition));
        Selected?.Invoke(position);
    }

    /// <summary>Shows the icons of the current profile; a null or empty path means the button has no icon.</summary>
    public void Refresh(Func<DeckPosition, string?> iconFilePathOf)
    {
        foreach (var (position, button) in _buttons)
        {
            button.ShowIcon(iconFilePathOf(position));
        }
    }

    private static string DefaultLabelOf(DeckPosition position) => position switch
    {
        DeckPosition.LeftUp => "UP",
        DeckPosition.LeftLeft => "LEFT",
        DeckPosition.LeftRight => "RIGHT",
        DeckPosition.LeftDown => "DOWN",
        DeckPosition.RightUp => "C",
        DeckPosition.RightLeft => "D",
        DeckPosition.RightRight => "A",
        DeckPosition.RightDown => "B",
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
    };
}
