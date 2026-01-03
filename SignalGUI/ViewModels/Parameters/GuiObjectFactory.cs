using System;
using System.Collections.Generic;
using System.Linq;
using SignalCore.Storage;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public class GuiObjectFactory
{
    private string _objectName;
    public Type ObjType { get; set; }
    public IDictionary<string, Type> Arguments { get; set; } = new Dictionary<string,Type>();
    public IDictionary<string, object> InstanceArguments { get; set; } = new Dictionary<string,object>();
    public string Name => _objectName;
    public GuiObjectFactory(Type objType, IDictionary<string, (Type type, object? defaultValue)> arguments,string? objectName = null)
    {
        ObjType = objType;
        Arguments = arguments.ToDictionary(v=>v.Key,v=>v.Value.type);
        // System.Console.WriteLine(objType.Name);
        // Initialize instance arguments with default values
        foreach (var arg in arguments)
        {
            var defaultArgument = arg.Value.defaultValue ??
                ArgumentsTypesUtils.GetDefaultValue(arg.Value.type);
            InstanceArguments[arg.Key] = defaultArgument;

            // System.Console.WriteLine($"{arg.Value.type.Name} {arg.Key} {InstanceArguments[arg.Key]} {arg.Value.defaultValue is null}");
        }
        _objectName = objectName ?? objType.Name;
    }

    public override string ToString()
    {
        return _objectName;
    }
    public object GetInstance()
        => new ObjectFactory(
                ObjType ?? throw new ArgumentException("Cannot get type"),
                InstanceArguments
            ).CreateInstance();
}