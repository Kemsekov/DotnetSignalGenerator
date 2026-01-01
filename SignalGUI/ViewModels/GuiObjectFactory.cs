using System;
using System.Collections.Generic;
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
    public GuiObjectFactory(Type objType, IDictionary<string, Type> arguments,string? objectName = null)
    {
        ObjType = objType;
        Arguments = arguments;

        // Initialize instance arguments with default values
        foreach (var arg in arguments)
        {
            InstanceArguments[arg.Key] = ArgumentsTypesUtils.GetDefaultValue(arg.Value);
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