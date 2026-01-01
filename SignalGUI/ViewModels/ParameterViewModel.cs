using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public partial class ParameterViewModel : ObservableObject
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public object Value { get; set; }

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

    public ParameterViewModel(string name, Type type, object value)
    {
        Name = name;
        Type = type;
        Value = value;
        StringValue = value?.ToString() ?? "";
    }

    public void UpdateValue()
    {
        try
        {
            Value = ArgumentsTypesUtils.ParseValue(Type, StringValue);
        }
        catch
        {
            // If parsing fails, keep the original value
            StringValue = Value?.ToString() ?? "";
        }
    }
}