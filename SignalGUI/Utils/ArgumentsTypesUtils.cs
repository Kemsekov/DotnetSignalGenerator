using System;
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
                return int.TryParse(stringValue, out int intResult) ? intResult : 0;
            }
            else if (type == typeof(float))
            {
                return float.TryParse(stringValue, out float floatResult) ? floatResult : 0.0f;
            }
            else if (type == typeof(double))
            {
                return double.TryParse(stringValue, out double doubleResult) ? doubleResult : 0.0;
            }
            else if (type == typeof(long))
            {
                return long.TryParse(stringValue, out long longResult) ? longResult : 0L;
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
    }
}