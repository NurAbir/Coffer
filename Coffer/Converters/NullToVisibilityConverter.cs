using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Coffer.Converters;

/// <summary>
/// Visible when the value is not null. Pass "Inverse" as the converter
/// parameter to show the element only when the value is null.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value is null;
        bool inverse = string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase);
        bool visible = inverse ? isNull : !isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}