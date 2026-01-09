using SignalCore.Storage;
using System.Text.Json;

namespace SignalTests;

public class ObjectFactoryTests
{
    // Test classes for testing ObjectFactory
    public class TestClassWithIntConstructor
    {
        public int Value { get; }
        public TestClassWithIntConstructor(int value)
        {
            Value = value;
        }
    }

    public class TestClassWithStringConstructor
    {
        public string Value { get; }
        public TestClassWithStringConstructor(string value)
        {
            Value = value ?? string.Empty;
        }
    }

    public class TestClassWithMultipleConstructors
    {
        public int IntValue { get; }
        public string StringValue { get; }
        public double DoubleValue { get; }

        public TestClassWithMultipleConstructors(int intValue, string stringValue, double doubleValue = 1.0)
        {
            IntValue = intValue;
            StringValue = stringValue;
            DoubleValue = doubleValue;
        }
    }

    public class TestClassWithFloatConstructor
    {
        public float Value { get; }
        public TestClassWithFloatConstructor(float value)
        {
            Value = value;
        }
    }

    public class TestClassWithComplexType
    {
        public TestClassWithIntConstructor InnerObject { get; }
        public string Name { get; }

        public TestClassWithComplexType(TestClassWithIntConstructor innerObject, string name)
        {
            InnerObject = innerObject;
            Name = name;
        }
    }

    [Fact]
    public void TestCreateInstanceWithIntConstructor()
    {
        var args = new Dictionary<string, object> { { "value", 42 } };
        var factory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);
        
        var instance = factory.CreateInstance<TestClassWithIntConstructor>();
        
        Assert.NotNull(instance);
        Assert.Equal(42, instance.Value);
    }

    [Fact]
    public void TestCreateInstanceWithStringConstructor()
    {
        var args = new Dictionary<string, object> { { "value", "Hello World" } };
        var factory = new ObjectFactory(typeof(TestClassWithStringConstructor), args);
        
        var instance = factory.CreateInstance<TestClassWithStringConstructor>();
        
        Assert.NotNull(instance);
        Assert.Equal("Hello World", instance.Value);
    }

    [Fact]
    public void TestCreateInstanceWithMultipleArguments()
    {
        var args = new Dictionary<string, object>
        {
            { "intValue", 100 },
            { "stringValue", "Test String" },
            { "doubleValue", 3.14 }
        };
        var factory = new ObjectFactory(typeof(TestClassWithMultipleConstructors), args);
        
        var instance = factory.CreateInstance<TestClassWithMultipleConstructors>();
        
        Assert.NotNull(instance);
        Assert.Equal(100, instance.IntValue);
        Assert.Equal("Test String", instance.StringValue);
        Assert.Equal(3.14, instance.DoubleValue);
    }

    [Fact]
    public void TestCreateInstanceWithTupleConstructor()
    {
        var args = new (string fieldName, object value)[]
        {
            ("value", 123)
        };
        var factory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);
        
        var instance = factory.CreateInstance<TestClassWithIntConstructor>();
        
        Assert.NotNull(instance);
        Assert.Equal(123, instance.Value);
    }

    [Fact]
    public void TestCreateInstanceWithFloatConstructor()
    {
        var args = new Dictionary<string, object> { { "value", 3.14f } };
        var factory = new ObjectFactory(typeof(TestClassWithFloatConstructor), args);
        
        var instance = factory.CreateInstance<TestClassWithFloatConstructor>();
        
        Assert.NotNull(instance);
        Assert.Equal(3.14f, instance.Value);
    }

    [Fact]
    public void TestJsonSerializationDeserialization()
    {
        var args = new Dictionary<string, object> { { "value", 999 } };
        var originalFactory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);

        var json = originalFactory.ToJson();
        var deserializedFactory = ObjectFactory.FromJson(json);

        Assert.Equal(originalFactory.TypeFullName, deserializedFactory.TypeFullName);
        Assert.Equal(originalFactory.ConstructorArguments.Count, deserializedFactory.ConstructorArguments.Count);

        var originalArg = originalFactory.ConstructorArguments.First();
        var deserializedArg = deserializedFactory.ConstructorArguments.First();

        Assert.Equal(originalArg.Key, deserializedArg.Key);
        Assert.Equal(originalArg.Value.TypeFullName, deserializedArg.Value.TypeFullName);
        // Note: After deserialization, the Instance may be a JsonElement, so we check that the factory can still create the correct instance
        // The actual type conversion happens during CreateInstance()

        // Test that deserialized factory can create the same instance
        var instance = deserializedFactory.CreateInstance<TestClassWithIntConstructor>();
        Assert.NotNull(instance);
        Assert.Equal(999, instance.Value);
    }

    [Fact]
    public void TestJsonSerializationDeserializationWithMultipleArgs()
    {
        var args = new Dictionary<string, object>
        {
            { "intValue", 42 },
            { "stringValue", "Test" },
            { "doubleValue", 2.71 }
        };
        var originalFactory = new ObjectFactory(typeof(TestClassWithMultipleConstructors), args);
        
        var json = originalFactory.ToJson();
        var deserializedFactory = ObjectFactory.FromJson(json);
        
        Assert.Equal(originalFactory.TypeFullName, deserializedFactory.TypeFullName);
        Assert.Equal(originalFactory.ConstructorArguments.Count, deserializedFactory.ConstructorArguments.Count);
        
        // Test that deserialized factory can create the same instance
        var instance = deserializedFactory.CreateInstance<TestClassWithMultipleConstructors>();
        Assert.NotNull(instance);
        Assert.Equal(42, instance.IntValue);
        Assert.Equal("Test", instance.StringValue);
        Assert.Equal(2.71, instance.DoubleValue);
    }

    [Fact]
    public void TestCreateInstanceFromJson()
    {
        var args = new Dictionary<string, object> { { "value", 777 } };
        var factory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);
        var json = factory.ToJson();
        
        var restoredFactory = ObjectFactory.FromJson(json);
        var instance = restoredFactory.CreateInstance<TestClassWithIntConstructor>();
        
        Assert.NotNull(instance);
        Assert.Equal(777, instance.Value);
    }

    [Fact]
    public void TestCreateInstanceWithComplexType()
    {
        var innerObj = new TestClassWithIntConstructor(42);
        var args = new Dictionary<string, object>
        {
            { "innerObject", innerObj },
            { "name", "Complex Test" }
        };
        var factory = new ObjectFactory(typeof(TestClassWithComplexType), args);
        
        var instance = factory.CreateInstance<TestClassWithComplexType>();
        
        Assert.NotNull(instance);
        Assert.NotNull(instance.InnerObject);
        Assert.Equal(42, instance.InnerObject.Value);
        Assert.Equal("Complex Test", instance.Name);
    }

    [Fact]
    public void TestNonGenericCreateInstance()
    {
        var args = new Dictionary<string, object> { { "value", 555 } };
        var factory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);
        
        var instance = factory.CreateInstance();
        
        Assert.NotNull(instance);
        Assert.IsType<TestClassWithIntConstructor>(instance);
        var typedInstance = (TestClassWithIntConstructor)instance;
        Assert.Equal(555, typedInstance.Value);
    }

    [Fact]
    public void TestConstructorNotFoundThrowsException()
    {
        var args = new Dictionary<string, object> { { "nonExistentParam", "value" } };
        var factory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);
        
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateInstance<TestClassWithIntConstructor>());
        Assert.Contains("No matching constructor found", exception.Message);
    }

    [Fact]
    public void TestTypeConversionInJsonDeserialization()
    {
        // Create a factory and serialize to JSON
        var args = new Dictionary<string, object> { { "value", 123 } };
        var factory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);
        var json = factory.ToJson();
        
        // Deserialize and recreate
        var deserializedFactory = ObjectFactory.FromJson(json);
        var instance = deserializedFactory.CreateInstance<TestClassWithIntConstructor>();
        
        Assert.Equal(123, instance.Value);
    }

    [Fact]
    public void TestDefaultConstructor()
    {
        var factory = new ObjectFactory();
        Assert.NotNull(factory);
        Assert.Equal("", factory.TypeFullName);
        Assert.Empty(factory.ConstructorArguments);
    }

    [Fact]
    public void TestConstructorWithObjectInstance()
    {
        var testInstance = new TestClassWithIntConstructor(99);
        var args = new Dictionary<string, object> { { "value", 88 } };
        var factory = new ObjectFactory(testInstance, args);
        
        var instance = factory.CreateInstance<TestClassWithIntConstructor>();
        
        Assert.NotNull(instance);
        Assert.Equal(88, instance.Value); // Should use the args value, not the original instance
    }

    [Fact]
    public void TestConstructorWithObjectInstanceAndTupleArgs()
    {
        var testInstance = new TestClassWithIntConstructor(99);
        var args = new (string fieldName, object value)[] { ("value", 77) };
        var factory = new ObjectFactory(testInstance, args);
        
        var instance = factory.CreateInstance<TestClassWithIntConstructor>();
        
        Assert.NotNull(instance);
        Assert.Equal(77, instance.Value);
    }

    [Fact]
    public void TestTypeFullNameProperty()
    {
        var args = new Dictionary<string, object> { { "value", 1 } };
        var factory = new ObjectFactory(typeof(TestClassWithIntConstructor), args);
        
        var type = factory.Type;
        Assert.Equal(typeof(TestClassWithIntConstructor), type);
        Assert.Contains("TestClassWithIntConstructor", factory.TypeFullName);
    }

    [Fact]
    public void TestArgumentTypePreservation()
    {
        var args = new Dictionary<string, object>
        {
            { "intValue", 100 },
            { "stringValue", "hello" },
            { "doubleValue", 3.14 }
        };
        var factory = new ObjectFactory(typeof(TestClassWithMultipleConstructors), args);

        // Check that argument types are preserved
        Assert.Equal(3, factory.ConstructorArguments.Count);

        var intValueArg = factory.ConstructorArguments["intValue"];
        var textValueArg = factory.ConstructorArguments["stringValue"];
        var numberValueArg = factory.ConstructorArguments["doubleValue"];

        Assert.Contains("System.Int32", intValueArg.TypeFullName);
        Assert.Contains("System.String", textValueArg.TypeFullName);
        Assert.Contains("System.Double", numberValueArg.TypeFullName);
    }

    [Fact]
    public void TestJsonRoundTripMaintainsTypes()
    {
        var args = new Dictionary<string, object>
        {
            { "intValue", 100 },
            { "stringValue", "test" },
            { "doubleValue", 1.23 }
        };
        var originalFactory = new ObjectFactory(typeof(TestClassWithMultipleConstructors), args);
        
        var json = originalFactory.ToJson();
        var restoredFactory = ObjectFactory.FromJson(json);
        
        // Verify types are maintained after round trip
        Assert.Equal(originalFactory.ConstructorArguments.Count, restoredFactory.ConstructorArguments.Count);
        
        var instance = restoredFactory.CreateInstance<TestClassWithMultipleConstructors>();
        Assert.Equal(100, instance.IntValue);
        Assert.Equal("test", instance.StringValue);
        Assert.Equal(1.23, instance.DoubleValue);
    }
}