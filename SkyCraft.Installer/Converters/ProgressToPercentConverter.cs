using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SkyCraft.Installer.Converters;


public class ProgressToPercentConverter : IValueConverter
{
    public static ProgressToPercentConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            // Округляем до целого и добавляем %
            return $"{Math.Round(doubleValue)}%";
        }
        return "0%";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Обратное преобразование не нужно
        throw new NotImplementedException();
    }
}