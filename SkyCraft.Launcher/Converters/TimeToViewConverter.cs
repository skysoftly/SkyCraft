using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SkyCraft.Launcher.Converters;


public class TimeToViewConverter : IValueConverter
{
    public static TimeToViewConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime time)
        {
            // Округляем до целого и добавляем %
            return $"{time.Day}.{time.Month}.{time.Year}";
        }
        return "...";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Обратное преобразование не нужно
        throw new NotImplementedException();
    }
}