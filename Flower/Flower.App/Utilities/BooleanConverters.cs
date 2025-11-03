using System;
using System.Globalization;
using Avalonia.Data.Converters;

public sealed class ObjectIsNotNullToBoolConverter : IValueConverter
{
    public static readonly ObjectIsNotNullToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
