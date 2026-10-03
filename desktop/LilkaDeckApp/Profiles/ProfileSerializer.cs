using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Models;

namespace LilkaDeckApp.Profiles;

/// <summary>
/// Converts between a profile document and the config.json format stored on the device.
/// </summary>
public static class ProfileSerializer
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static byte[] Serialize(ProfileDocument document) =>
        JsonSerializer.SerializeToUtf8Bytes(ToDto(document), WriteOptions);

    public static bool TryDeserialize(
        string json,
        [NotNullWhen(true)] out ProfileDocument? document,
        [NotNullWhen(false)] out string? error)
    {
        document = null;
        try
        {
            var dto = JsonSerializer.Deserialize<ProfileConfigDto>(json);
            if (dto == null)
            {
                error = "The config is empty.";
                return false;
            }

            document = FromDto(dto);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    // Buttons are written in a fixed order, and only the ones that are actually set up.
    public static ProfileConfigDto ToDto(ProfileDocument document)
    {
        var dto = new ProfileConfigDto { ProfileName = document.Name, ActiveColor = document.ActiveColor };

        foreach (var position in DeckPositions.All)
        {
            if (document.Buttons.TryGetValue(position, out var button) && !button.IsEmpty)
            {
                dto.Buttons[position.ToWireName()] = ToDto(button);
            }
        }
        return dto;
    }

    private static ButtonDto ToDto(ButtonDefinition button) => new()
    {
        Icon = button.IconFileName,
        Type = button.ActionType.ToWireName(),
        Action = button.Actions.ToList()
    };

    // Missing or null fields fall back to defaults; buttons at unknown positions are skipped.
    private static ProfileDocument FromDto(ProfileConfigDto dto)
    {
        var buttons = new Dictionary<DeckPosition, ButtonDefinition>();

        foreach (var (name, button) in dto.Buttons ?? new Dictionary<string, ButtonDto>())
        {
            if (button is not null && DeckPositions.TryParse(name, out var position))
            {
                buttons[position] = FromDto(button);
            }
        }

        return new ProfileDocument(
            dto.ProfileName ?? ProfileDocument.DefaultName,
            dto.ActiveColor ?? ProfileDocument.DefaultActiveColor,
            buttons);
    }

    private static ButtonDefinition FromDto(ButtonDto button) => new(
        button.Icon ?? "",
        ActionTypes.Parse(button.Type),
        ActionTokens.Clean(button.Action ?? Enumerable.Empty<string>()));
}
