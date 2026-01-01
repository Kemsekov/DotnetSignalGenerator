using System.Reflection;
using System.Text.Json;

// The idea is following, using ObjectFactory we encode and save to DB
// all dependencies for signal creations, such as filters, generators, transforms, StringExpression etc
// For user session we do save all these objects.
// Then we use DynamicExpresso, reference these objects, and use them to create,
// modify,transform signal, as string expression. By doing this we
// give user full flexibility of signal creation, and meanwhile
// it is easy to save/load simple strings like a source code of a program.

/// <summary>
/// User can create and sum multiple signals
/// </summary>
namespace SignalCore.Storage;

public class ObjectFactory
{
    public class Argument
    {
        public required object Instance { get; set; }
        public required string TypeFullName { get; set; }
        
        public Type Type
        {
            get => GetTypeFromFullName(TypeFullName);
            set => TypeFullName = GetTypeFullName(value);
        }
    }
    public static string GetTypeFullName(Type t)
        => t.AssemblyQualifiedName ?? throw new Exception($"Cannot deduce assembly type name of type {t.Name}");
    public string TypeFullName { get; set; } = ""; // type.AssemblyQualifiedName
    public Type Type => GetTypeFromFullName(TypeFullName);
    public IDictionary<string, Argument> ConstructorArguments { get; set; } = new Dictionary<string,Argument>();
    
    // Default constructor
    public ObjectFactory() : this("", new Dictionary<string, object>()) { }
    
    // Constructors with Dictionary<string, object> arguments
    public ObjectFactory(string typeFullName, IDictionary<string, object> args)
    {
        TypeFullName = typeFullName;
        ConstructorArguments = ConvertArgsToArguments(args);
    }
    
    public ObjectFactory(Type type, IDictionary<string, object> args)
        : this(GetTypeFullName(type), args) { }
    
    public ObjectFactory(object instance, IDictionary<string, object> args)
        : this(instance.GetType(), args) { }
    
    // Constructors with (string, object)[] arguments
    public ObjectFactory(string typeFullName, (string fieldName, object value)[] args)
        : this(typeFullName, ConvertTupleArgsToDictionary(args)) { }
    
    public ObjectFactory(Type type, (string fieldName, object value)[] args)
        : this(type, ConvertTupleArgsToDictionary(args)) { }
    
    public ObjectFactory(object instance, (string fieldName, object value)[] args)
        : this(instance.GetType(), args) { }
    
    // Generic creation method
    public T CreateInstance<T>()
    {
        var result = CreateInstance();
        if (result is T typedResult)
            return typedResult;
        
        throw new Exception($"Failed to cast created object to type {typeof(T).Name}");
    }
    
    // Non-generic creation method
    public object CreateInstance()
    {
        var type = GetTypeFromFullName(TypeFullName);
        
        var constructorTypes = ConstructorArguments.Values
            .Select(arg => GetTypeFromFullName(arg.TypeFullName))
            .ToArray();
        
        var constructorInfo = type.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            null,
            constructorTypes,
            null
        );
        
        if (constructorInfo == null)
        {
            throw new InvalidOperationException($"No matching constructor found for type: {type.FullName}");
        }
        
        var constructorParameters = ConstructorArguments.Values
            .Select(arg => DeserializeIfJsonElement(arg.Instance, GetTypeFromFullName(arg.TypeFullName)!))
            .ToArray();
        
        return constructorInfo.Invoke(constructorParameters);
    }
    
    // Serialization methods
    public string ToJson()=>JsonSerializer.Serialize(this);
    
    public static ObjectFactory FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<ObjectFactory>(json, options)
               ?? throw new ArgumentException($"Cannot deserialize JSON to ObjectFactory: {json}");
    }
    
    // Private helper methods
    private static Type GetTypeFromFullName(string typeFullName)=>
     Type.GetType(typeFullName) ?? throw new ArgumentException($"Type not found: {typeFullName}");
    
    
    private static Dictionary<string, Argument> ConvertArgsToArguments(IDictionary<string, object> args)
        => args.ToDictionary(
            v => v.Key,
            v => new Argument
            {
                Instance = v.Value,
                TypeFullName = GetTypeFullName(v.Value?.GetType() ?? throw new ArgumentException("Cannot get type of null value"))
            }
        );
    private static Dictionary<string, object> ConvertTupleArgsToDictionary((string fieldName, object value)[] args)
        => args.ToDictionary(v => v.fieldName, v => v.value);
    
    private static object DeserializeIfJsonElement(object value, Type targetType)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.Deserialize(targetType) ?? throw new ArgumentException($"Failed to convert {value} to type {targetType.Name}");
        }
        return value;
    }
}