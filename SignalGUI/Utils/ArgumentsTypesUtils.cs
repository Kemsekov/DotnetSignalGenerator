using System;
using System.Globalization;
using System.Linq;

namespace SignalGUI.Utils
{
    public static class ArgumentsTypesUtils
    {
        public static Type[] SupportedTypes = [
            typeof(float),
            typeof(double),
            typeof(int),
            typeof(long),
            typeof(string),
        ];
        /// <summary>
        /// Parses a string value to the specified type
        /// </summary>
        /// <param name="type">The target type to parse to</param>
        /// <param name="stringValue">The string value to parse</param>
        /// <returns>The parsed value or the original value if parsing fails</returns>
        public static object ParseValue(Type type, string stringValue)
        {
            if (type == typeof(int))
            {
                return int.TryParse(stringValue, out int intResult) ? intResult : stringValue;
            }
            else if (type == typeof(float))
            {
                return float.TryParse(stringValue, out float floatResult) ? floatResult : stringValue;
            }
            else if (type == typeof(double))
            {
                return double.TryParse(stringValue, out double doubleResult) ? doubleResult : stringValue;
            }
            else if (type == typeof(long))
            {
                return long.TryParse(stringValue, out long longResult) ? longResult : stringValue;
            }
            else if (type == typeof(string))
            {
                return stringValue;
            }
            
            // For other types, return the string value as-is
            return stringValue;
        }

        /// <summary>
        /// Gets the default value for a specified type
        /// </summary>
        /// <param name="type">The type to get default value for</param>
        /// <returns>The default value for the type</returns>
        public static object GetDefaultValue(Type type)
        {
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0.0f;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(long)) return 0L;
            if (type == typeof(string)) return "";
            
            throw new ArgumentException($"Cannot get default value of type {type.Name}");
        }
        
        /// <summary>
        /// Checks if the type is one of the supported types for parsing
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is supported for parsing, false otherwise</returns>
        public static bool IsSupportedType(Type type)
        {
            return SupportedTypes.Any(v=>v==type);
        }

    public static object? CastOrThrow(this object? v, Type t,Exception? e = null)
    {
        if (t is null) throw new ArgumentNullException(nameof(t));

        // 1) Already the right runtime type (or null allowed for reference/nullable)
        if (v is null)
        {
            if (!t.IsValueType || Nullable.GetUnderlyingType(t) != null) return null;
            throw e ?? new InvalidCastException($"Cannot cast null to non-nullable value type {t.FullName}.");
        }
        if (t.IsInstanceOfType(v)) return v;

        var vt = v.GetType();

        // 2) “Cast” in the assignability sense (base class / interface)
        // If this is true, the object is already compatible; the earlier IsInstanceOfType
        // should have caught most cases, but keep it as a sanity check.
        if (t.IsAssignableFrom(vt)) return v; // assignable means you can treat it as t [web:48]

        // 3) Conversions
        // Unwrap Nullable<T> target to T for conversion. [web:37][web:38]
        var nonNullableTarget = Nullable.GetUnderlyingType(t) ?? t;

        try
        {
            // Enums: allow "string name" or "numeric underlying value".
            if (nonNullableTarget.IsEnum)
            {
                if (v is string s)
                    return Enum.Parse(nonNullableTarget, s, ignoreCase: true);

                var underlying = Enum.GetUnderlyingType(nonNullableTarget);
                var numeric = Convert.ChangeType(v, underlying, CultureInfo.InvariantCulture); // [web:38]
                return Enum.ToObject(nonNullableTarget, numeric!);
            }

            // General IConvertible conversions (numbers, DateTime, etc.). [web:38]
            if (v is IConvertible)
                return Convert.ChangeType(v, nonNullableTarget, CultureInfo.InvariantCulture);

            // If you want: add a custom converter hook here (TypeConverter) for non-IConvertible types.
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException || ex is ArgumentException)
        {
            throw e ?? new  InvalidCastException($"Cannot cast/convert value of type {vt.FullName} to {t.FullName}.", ex);
        }

        throw e ?? new InvalidCastException($"Cannot cast/convert value of type {vt.FullName} to {t.FullName}.");
    }
}
}