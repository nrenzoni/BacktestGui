using System;
using System.Globalization;
using Avalonia.Data.Converters;
using NodaTime;
using NodaTime.Extensions;

namespace BacktestGui.XamlHelpers;

public class LocalDateToDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;
        var date = (LocalDate)value;
        return date.ToDateTimeUnspecified();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;
        var date = (DateTime)value;
        return date.ToLocalDateTime().Date;
    }
}