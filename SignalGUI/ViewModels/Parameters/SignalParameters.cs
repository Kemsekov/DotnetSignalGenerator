using System;
using System.Collections.Generic;
using SignalCore;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public class SignalParameters
{
    public int Points { get; set; }

    public SignalParameters(int points = 256)
    {
        Points = points;
    }

    public static GuiObjectFactory CreateFactory()
    {

        var type = typeof(SignalParameters);
        var factory = new GuiObjectFactory(
            type,
            type.GetSupportedConstructor(ArgumentsTypesUtils.SupportedTypes)
            ?? throw new ArgumentException("Failed to find supported constructor"), 
            "Signal Parameters"
        );
        
        // Set default values
        factory.InstanceArguments["points"] = 256;

        return factory;
    }
}