using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Coffer.Converters;

public class StrengthToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Weak = new(Color.FromRgb(0xF2, 0x6D, 0x6D));
    private static readonly SolidColorBrush Fair = new(Color.FromRgb(0xF5, 0xB7, 0x59));
    private static readonly SolidColorBrush Good = new(Color.FromRgb(0xE0, 0xB1, 0x5C));
    private static readonly SolidColorBrush Strong = new(Color.FromRgb(0x3D, 0xD6, 0xB3));
    private static readonly SolidColorBrush VeryStrong = new(Color.FromRgb(0x72, 0xE8, 0xB6));
    private static readonly SolidColorBrush Neutral = new(Color.FromRgb(0x8B, 0x98, 0xA9));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "Weak" => Weak,
            "Fair" => Fair,
            "Good" => Good,
            "Strong" => Strong,
            "Very strong" or "Very Strong" => VeryStrong,
            _ => Neutral
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}