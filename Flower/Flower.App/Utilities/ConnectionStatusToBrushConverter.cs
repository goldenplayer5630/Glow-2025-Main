using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Flower.Core.Enums;

namespace Flower.App.Utilities
{
    public sealed class ConnectionStatusToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var brush = value is ConnectionStatus s
                ? s switch
                {
                    ConnectionStatus.Connected => Brushes.Green,
                    ConnectionStatus.Degraded => Brushes.Orange,
                    ConnectionStatus.Disconnected => Brushes.Red,
                    _ => Brushes.Gray
                }
                : Brushes.Gray;

            // If target requests Color, give Color; if it asks for Brush, give Brush.
            if (targetType == typeof(Color))
                return ((ISolidColorBrush)brush).Color;

            return brush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Avalonia.Data.BindingOperations.DoNothing;
    }
}
