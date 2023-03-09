using System;
using System.Globalization;
using Avalonia.Data.Converters;
using BacktestGui.Models;
using CustomShared;

namespace BacktestGui.XamlHelpers;

public class DecimalWithInfToStrConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        if (value is not DecimalWithInf decimalWithInf)
            throw new Exception(
                $"{nameof(DecimalWithInfToStrConverter)}.{nameof(Convert)} only works on type {nameof(DecimalWithInf)}");

        if (parameter is not null && parameter is not int)
            throw new Exception();

        if (decimalWithInf.Value is not null)
        {
            var parameterAsInt = parameter as int? ?? 2;

            var formatter = "0." + new string('#', parameterAsInt);

            return decimalWithInf.Value.Value.ToString(formatter);
        }

        return decimalWithInf.NegativeInfinity
            ? "-inf"
            : "inf";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            "-inf" => decimal.MinValue,
            "inf" => decimal.MaxValue,
            _ => System.Convert.ToDecimal(value)
        };
    }
}