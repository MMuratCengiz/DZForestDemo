using System.Globalization;
using Avalonia.Data.Converters;

namespace NiziKit.Editor.ViewModels;

public class ImportTypeConverter : IValueConverter
{
    public static readonly ImportTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ImportType importType && parameter is ImportType paramType)
        {
            return importType == paramType;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is ImportType paramType)
        {
            return paramType;
        }
        return ImportType.Both;
    }
}
