namespace LilkaDeckApp.Domain;

public static class HexColor
{
    /// <summary>Formats a color as "#RRGGBB", the form config.json and the firmware use.</summary>
    public static string Format(byte red, byte green, byte blue) => $"#{red:X2}{green:X2}{blue:X2}";
}
