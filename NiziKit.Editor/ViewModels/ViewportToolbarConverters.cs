using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NiziKit.Editor.ViewModels;

public static class ViewportToolbarConverters
{
    public static readonly IValueConverter Is2DToIconConverter = new Is2DToIconConverterImpl();
    public static readonly IValueConverter FpsToColorConverter = new FpsToColorConverterImpl();

    private class Is2DToIconConverterImpl : IValueConverter
    {
        // 3D cube icon
        private static readonly StreamGeometry Icon3D = StreamGeometry.Parse(
            "M21 16.5c0 .38-.21.71-.53.88l-7.9 4.44c-.16.12-.36.18-.57.18-.21 0-.41-.06-.57-.18l-7.9-4.44A.991.991 0 0 1 3 16.5v-9c0-.38.21-.71.53-.88l7.9-4.44c.16-.12.36-.18.57-.18.21 0 .41.06.57.18l7.9 4.44c.32.17.53.5.53.88v9zM12 4.15L6.04 7.5 12 10.85l5.96-3.35L12 4.15zM5 15.91l6 3.38v-6.71L5 9.21v6.7zm14 0v-6.7l-6 3.37v6.71l6-3.38z");

        // 2D square icon
        private static readonly StreamGeometry Icon2D = StreamGeometry.Parse(
            "M3 3h18v18H3V3zm2 2v14h14V5H5z");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? Icon2D : Icon3D;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    private class FpsToColorConverterImpl : IValueConverter
    {
        private static readonly SolidColorBrush GoodFps = new(Color.FromRgb(85, 204, 85));    // Green
        private static readonly SolidColorBrush OkFps = new(Color.FromRgb(224, 180, 85));     // Yellow
        private static readonly SolidColorBrush BadFps = new(Color.FromRgb(224, 85, 85));     // Red

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is float fps)
            {
                if (fps >= 60)
                {
                    return GoodFps;
                }

                if (fps >= 30)
                {
                    return OkFps;
                }

                return BadFps;
            }
            return GoodFps;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
