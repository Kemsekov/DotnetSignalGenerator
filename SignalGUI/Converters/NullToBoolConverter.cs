using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SignalGUI.Converters;

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool invert = false;

        // Check if we want to invert the result (if parameter is true)
        if (parameter is string paramStr && bool.TryParse(paramStr, out bool paramBool))
        {
            invert = paramBool;
        }

        bool isNull = value == null;

        // If invert is true, return true when value is NOT null (to show the element when NOT null)
        // If invert is false, return true when value IS null (to show the element when null)
        bool result = invert ? !isNull : isNull;

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This converter is one-way only
        return null;
    }
}