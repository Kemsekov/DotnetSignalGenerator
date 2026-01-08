using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public partial class ParameterViewModelWithCallback : ObservableObject
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public object Value { get; set; }
    private Action<object> _updateCallback;
    private string _stringValue = "";
    public string StringValue
    {
        get => _stringValue;
        set
        {
            if (SetProperty(ref _stringValue, value))
            {
                UpdateValue();
            }
        }
    }

    public ParameterViewModelWithCallback(string name, Type type, object value, Action<object> updateCallback)
    {
        Name = name;
        Type = type;
        Value = value;
        _updateCallback = updateCallback;
        StringValue = value?.ToString() ?? "";
    }

    public void UpdateValue()
    {
        try
        {
            Value = ArgumentsTypesUtils.ParseValue(Type, StringValue);
            // Call the callback to update the factory's InstanceArguments
            _updateCallback?.Invoke(Value);
        }
        catch(Exception e)
        {
            // If parsing fails, show error
            Dispatcher.UIThread.Post(()=>ErrorHandlingUtils.ShowErrorWindow(e));
        }
    }
}