using System;
using System.Collections.Generic;
using SignalCore;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public class SignalParameters
{
    public int ComputePoints { get; set; }
    public int RenderPoints { get; set; }

    public SignalParameters(int computePoints = 1024,int renderPoints=256)
    {
        if(computePoints<=0 || renderPoints<=0)
            throw new ArgumentException("computePoints and renderPoints must be positive numbers!");
        ComputePoints = computePoints;
        RenderPoints=renderPoints;
    }

    public static GuiObjectFactory CreateFactory()
    {
        var ctor = 
            typeof(SignalParameters)
            .GetSupportedConstructor(ArgumentsTypesUtils.SupportedTypes);
        return new GuiObjectFactory(typeof(SignalParameters),ctor ?? throw new Exception());
    }
}