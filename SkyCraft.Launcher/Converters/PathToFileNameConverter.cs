using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;

namespace SkyCraft.Launcher.Converters;


public class PathToFileNameConverter : IValueConverter
{
    public static PathToFileNameConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path)
        {
            return $"{Path.GetFileName(path)}";
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Обратное преобразование не нужно
        throw new NotImplementedException();
    }
}