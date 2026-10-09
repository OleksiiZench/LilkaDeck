using LilkaDeckApp.Domain;

namespace LilkaDeckApp.Profiles;

/// <summary>What the button editor shows: the icon's file name, the type of the action and the action text as it was last set.</summary>
public sealed record ButtonEditState(string IconFileName, ActionType ActionType, string ActionText);

/// <summary>Where the button editor reads and keeps the settings of one deck button.</summary>
public interface IButtonStore
{
    ButtonEditState GetButton(DeckPosition position);

    void SetActionType(DeckPosition position, ActionType type);

    void SetActionText(DeckPosition position, string text);
}
