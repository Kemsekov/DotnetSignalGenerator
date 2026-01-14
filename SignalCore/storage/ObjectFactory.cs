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

public class ObjectFactory : ICloneable
{
    public class Argument
    {
        public required object? Instance { get; set; }
        public string TypeFullName { get; set; }="";
        
        [System.Text.Json.Serialization.JsonIgnore]
        public Type Type
        {
            get => GetTypeFromFullName(TypeFullName);
            set => TypeFullName = GetTypeFullName(value);
        }
    }
    public static string GetTypeFullName(Type t)
        => t.AssemblyQualifiedName ?? throw new Exception($"Cannot deduce assembly type name of type {t.Name}");
    public string TypeFullName { get; set; } = ""; // type.AssemblyQualifiedName
    public string ObjectName{get;set;} = "";
    [System.Text.Json.Serialization.JsonIgnore]
    public Type Type => GetTypeFromFullName(TypeFullName);
    public IDictionary<string, Argument> ConstructorArguments { get; set; } = new Dictionary<string,Argument>();
    
    // Default constructor
    public ObjectFactory() : this("", new Dictionary<string, object>()) { }
    
    // Constructors with Dictionary<string, object> arguments
    public ObjectFactory(string typeFullName, IDictionary<string, object> args)
    {
        TypeFullName = typeFullName;
        ConstructorArguments = ConvertArgsToArguments(args);
        ValidateInstanceArgumentsType();
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
    public ObjectFactory(Type type, IDictionary<string, Argument> args)
    {
        TypeFullName = GetTypeFullName(type);
        ConstructorArguments = args;
        ValidateInstanceArgumentsType();
    }
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
        ValidateInstanceArgumentsType();

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
        //once again check and reparse
        var constructorParameters = ConstructorArguments.Values
            .Select(arg => arg.Instance)
            .ToArray();
        
        return constructorInfo.Invoke(constructorParameters);
    }
    public void ValidateInstanceArgumentsType()
    {
        foreach(var key in ConstructorArguments.Keys)
        {
            var value = ConstructorArguments[key];
            var type = value.Type;
            ConstructorArguments[key].Instance=value.Instance.CastOrThrow(
                type, new ArgumentException($"Cannot use \"{value.Instance}\" as value for field \"{key}\" with type {type.Name}")
            ) ?? throw new ArgumentException($"Parameter {key} cannot be null!");
        }
    }
    // Serialization methods
    public string ToJson()=>JsonSerializer.Serialize(this);
    
    public static ObjectFactory FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<ObjectFactory>(json, options)
               ?? throw new ArgumentException($"Cannot deserialize JSON to ObjectFactory: {json}");
        
        // parse json element objects to a proper types
        result.ValidateInstanceArgumentsType();
        
        return result;
    }
    
    // Private helper methods
    static Type GetTypeFromFullName(string typeFullName)=>
     Type.GetType(typeFullName) ?? throw new ArgumentException($"Type not found: {typeFullName}");
    
    static Dictionary<string, Argument> ConvertArgsToArguments(IDictionary<string, object> args)
        => args.ToDictionary(
            v => v.Key,
            v => new Argument
            {
                Instance = v.Value,
                TypeFullName = GetTypeFullName(v.Value?.GetType() ?? throw new ArgumentException("Cannot get type of null value"))
            }
        );
    static Dictionary<string, object> ConvertTupleArgsToDictionary((string fieldName, object value)[] args)
        => args.ToDictionary(v => v.fieldName, v => v.value);
    public ObjectFactory Clone()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return new(Type,new Dictionary<string, object>())
        {
            ConstructorArguments = ConstructorArguments.ToDictionary(
                v=>v.Key,
                v=>new Argument
                {
                    TypeFullName=v.Value.TypeFullName,
                    Instance = 
                        JsonSerializer.Deserialize(JsonSerializer.Serialize(v.Value.Instance), v.Value.Type,options)
                        ?? v.Value.Instance
                }
            )
        };
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}