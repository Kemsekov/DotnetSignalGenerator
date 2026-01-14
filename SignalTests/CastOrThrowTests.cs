using SignalCore;
using System;
using System.Text.Json;

namespace SignalTests;

public class CastOrThrowTests
{
    // Test enum for enum conversion tests
    public enum TestEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    [Fact]
    public void CastOrThrow_BasicNumericConversions()
    {
        // Test int to float
        var result = 42.CastOrThrow(typeof(float));
        Assert.Equal(42.0f, result);
        Assert.IsType<float>(result);

        // Test int to double
        result = 42.CastOrThrow(typeof(double));
        Assert.Equal(42.0, result);
        Assert.IsType<double>(result);

        // Test int to long
        result = 42.CastOrThrow(typeof(long));
        Assert.Equal(42L, result);
        Assert.IsType<long>(result);

        // Test float to double
        result = 3.14f.CastOrThrow(typeof(double));
        Assert.Equal(3.14, (double)result, 2);
        Assert.IsType<double>(result);

        // Test double to float
        result = 3.14.CastOrThrow(typeof(float));
        Assert.Equal(3.14f, result);
        Assert.IsType<float>(result);
    }

    [Fact]
    public void CastOrThrow_SameTypeReturnsOriginal()
    {
        var original = 42;
        var result = original.CastOrThrow(typeof(int));
        Assert.Equal(original, result);
        // For value types, Same assertion doesn't work as they are copied
        Assert.Equal(42, result);
    }

    [Fact]
    public void CastOrThrow_AssignableTypesReturnOriginal()
    {
        var original = "Hello";
        var result = original.CastOrThrow(typeof(object)); // string is assignable to object
        Assert.Equal(original, result);
        Assert.Same(original, result);
    }

    [Fact]
    public void CastOrThrow_JsonElementDeserialization()
    {
        using var document = JsonDocument.Parse("42");
        var jsonElement = document.RootElement;

        var result = jsonElement.CastOrThrow(typeof(int));
        Assert.Equal(42, result);
        Assert.IsType<int>(result);

        using var document2 = JsonDocument.Parse("\"Hello World\"");
        var jsonElement2 = document2.RootElement;

        var result2 = jsonElement2.CastOrThrow(typeof(string));
        Assert.Equal("Hello World", result2);
        Assert.IsType<string>(result2);

        using var document3 = JsonDocument.Parse("true");
        var jsonElement3 = document3.RootElement;

        var result3 = jsonElement3.CastOrThrow(typeof(bool));
        Assert.True((bool)result3);
        Assert.IsType<bool>(result3);
    }

    [Fact]
    public void CastOrThrow_StringParsingConversions()
    {
        // Test string to int
        var result = "42".CastOrThrow(typeof(int));
        Assert.Equal(42, result);
        Assert.IsType<int>(result);

        // Test string to float
        result = "3.14".CastOrThrow(typeof(float));
        Assert.Equal(3.14f, result);
        Assert.IsType<float>(result);

        // Test string to double
        result = "2.71".CastOrThrow(typeof(double));
        Assert.Equal(2.71, result);
        Assert.IsType<double>(result);

        // Test string to long
        result = "1000".CastOrThrow(typeof(long));
        Assert.Equal(1000L, result);
        Assert.IsType<long>(result);

        // Test string to string (should return as-is)
        result = "Hello".CastOrThrow(typeof(string));
        Assert.Equal("Hello", result);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void CastOrThrow_EnumConversions()
    {
        // Test string to enum
        var result = "Value2".CastOrThrow(typeof(TestEnum));
        Assert.Equal(TestEnum.Value2, result);
        Assert.IsType<TestEnum>(result);

        // Test numeric value to enum
        result = 2.CastOrThrow(typeof(TestEnum));
        Assert.Equal(TestEnum.Value2, result);
        Assert.IsType<TestEnum>(result);

        // Test string with different case to enum
        result = "value1".CastOrThrow(typeof(TestEnum));
        Assert.Equal(TestEnum.Value1, result);
        Assert.IsType<TestEnum>(result);
    }

    [Fact]
    public void CastOrThrow_ThrowsOnNullValue()
    {
        object? nullValue = null;
        var exception = Assert.Throws<InvalidCastException>(() => nullValue.CastOrThrow(typeof(int)));
        Assert.Contains("Cannot cast null", exception.Message);
    }

    [Fact]
    public void CastOrThrow_ThrowsOnImpossibleConversion()
    {
        // Try to convert an incompatible type
        var exception = Assert.Throws<InvalidCastException>(() => "not_a_number".CastOrThrow(typeof(int)));
        Assert.Contains("Cannot cast/convert value", exception.Message);
    }

    [Fact]
    public void CastOrThrow_WithCustomException()
    {
        var customException = new ArgumentException("Custom error message");
        var exception = Assert.Throws<ArgumentException>(() => "not_a_number".CastOrThrow(typeof(int), customException));
        Assert.Equal("Custom error message", exception.Message);
    }

    [Fact]
    public void CastOrThrow_FallbackToParseValueWhenDirectConversionFails()
    {
        // Test that string representations get parsed correctly
        var result = "123".CastOrThrow(typeof(int));
        Assert.Equal(123, result);
        Assert.IsType<int>(result);

        // Test invalid string that should throw an exception based on the current implementation
        var exception = Assert.Throws<InvalidCastException>(() => "abc".CastOrThrow(typeof(int)));
        Assert.Contains("Cannot cast/convert value", exception.Message);
    }

    [Fact]
    public void CastOrThrow_ConversionOverflow()
    {
        // Test conversion that would overflow
        var largeLong = long.MaxValue;
        var exception = Assert.Throws<InvalidCastException>(() => largeLong.CastOrThrow(typeof(int)));
        Assert.Contains("Cannot cast/convert value", exception.Message);
    }

    [Fact]
    public void CastOrThrow_BoolConversions()
    {
        // Test string to bool
        var result = "true".CastOrThrow(typeof(bool));
        Assert.True((bool)result);
        Assert.IsType<bool>(result);

        result = "True".CastOrThrow(typeof(bool));
        Assert.True((bool)result);
        Assert.IsType<bool>(result);

        result = "false".CastOrThrow(typeof(bool));
        Assert.False((bool)result);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void CastOrThrow_DateTimeConversions()
    {
        // Test string to DateTime
        var dateString = "2023-01-01T10:00:00";
        var result = dateString.CastOrThrow(typeof(DateTime));
        var expected = DateTime.Parse(dateString);
        Assert.Equal(expected, result);
        Assert.IsType<DateTime>(result);
    }

    [Fact]
    public void CastOrThrow_NullableTypeConversions()
    {
        // Test conversion to nullable types
        var result = 42.CastOrThrow(typeof(int?));
        Assert.Equal(42, result);
        Assert.IsType<int>(result); // Underlying type, not nullable

        var result2 = "123".CastOrThrow(typeof(int?));
        Assert.Equal(123, result2);
        Assert.IsType<int>(result2);
    }

    [Fact]
    public void CastOrThrow_CharConversions()
    {
        // Test char to int
        var result = 'A'.CastOrThrow(typeof(int));
        Assert.Equal(65, result); // ASCII value of 'A'
        Assert.IsType<int>(result);

        // Test string to char
        result = "B".CastOrThrow(typeof(char));
        Assert.Equal('B', result);
        Assert.IsType<char>(result);
    }

    [Fact]
    public void CastOrThrow_DecimalConversions()
    {
        // Test int to decimal
        var result = 42.CastOrThrow(typeof(decimal));
        Assert.Equal(42m, result);
        Assert.IsType<decimal>(result);

        // Test string to decimal
        result = "123.45".CastOrThrow(typeof(decimal));
        Assert.Equal(123.45m, result);
        Assert.IsType<decimal>(result);
    }

    [Fact]
    public void CastOrThrow_ByteArrayConversions()
    {
        // Since byte[] is not in the supported types, this should throw
        var base64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello"));
        var exception = Assert.Throws<InvalidCastException>(() => base64String.CastOrThrow(typeof(byte[])));
        Assert.Contains("Cannot cast/convert value", exception.Message);
    }

    [Fact]
    public void CastOrThrow_GuidConversions()
    {
        // Since Guid is not in the supported types, this should throw
        var guidString = Guid.NewGuid().ToString();
        var exception = Assert.Throws<InvalidCastException>(() => guidString.CastOrThrow(typeof(Guid)));
        Assert.Contains("Cannot cast/convert value", exception.Message);
    }

    [Fact]
    public void CastOrThrow_TimeSpanConversions()
    {
        // Since TimeSpan is not in the supported types, this should throw
        var timeString = "01:30:45";
        var exception = Assert.Throws<InvalidCastException>(() => timeString.CastOrThrow(typeof(TimeSpan)));
        Assert.Contains("Cannot cast/convert value", exception.Message);
    }
}