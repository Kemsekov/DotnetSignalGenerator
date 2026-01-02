using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SignalGUI.Converters;

public class BoolToOnOffConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "ON " : "OFF";
        }
        return "OFF";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return stringValue == "ON ";
        }
        return false;
    }
}