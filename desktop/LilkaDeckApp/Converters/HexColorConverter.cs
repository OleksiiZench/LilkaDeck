using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LilkaDeckApp.Domain;

namespace LilkaDeckApp.Converters;

/// <summary>Lets a color picker edit a color that the view model keeps as "#RRGGBB" text.</summary>
public sealed class HexColorConverter : IValueConverter
{
    public static HexColorConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string hex && Color.TryParse(hex, out var color) ? color : BindingOperations.DoNothing;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color color ? HexColor.Format(color.R, color.G, color.B) : BindingOperations.DoNothing;
}
