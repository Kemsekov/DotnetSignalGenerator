using SignalCore.Storage;

namespace SignalCore;

public static class Reflection
{
    /// <summary>
    /// Get types of all implementations of given type
    /// </summary>
    public static Type[] GetAllImplementations(this Type interfaceType)
    {
        var assembly = interfaceType.Assembly;
        return assembly.GetTypes()
            .Where(type => type.IsClass &&
                          !type.IsAbstract &&
                          interfaceType.IsAssignableFrom(type))
            .ToArray();
    }
    /// <summary>
    /// Finds largest constructor with largest number of arguments of given type
    /// </summary>
    public static Dictionary<string,ObjectFactory.Argument>? GetSupportedConstructor(this Type type,Type[] allowedConstructorTypes)
    {
        var constructors = type.GetConstructors();
        var result = new List<Dictionary<string, ObjectFactory.Argument>>();

        for (int i = 0; i < constructors.Length; i++)
        {
            var constructor = constructors[i];
            
            var parameters = constructor.GetParameters();
            
            var paramTypes = parameters.Select(v=>v.ParameterType);
            if(paramTypes.Any(v=>!allowedConstructorTypes.Contains(v))) continue;

            var paramDict = new Dictionary<string, ObjectFactory.Argument>();

            foreach (var param in parameters)
            {
                if(param?.Name is null) continue;
                var paramDef = (param.DefaultValue==DBNull.Value) ? null : param.DefaultValue;
                paramDict[param.Name] = new ObjectFactory.Argument{
                    Type=param.ParameterType,
                    Instance=paramDef
                };
            }

            result.Add(paramDict);
        }
        if(result.Count==0) return null;
        return result.MaxBy(v=>v.Keys.Count);
    }
}