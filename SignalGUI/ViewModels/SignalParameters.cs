using System;
using System.Collections.Generic;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public class SignalParameters
{
    public float TStart { get; set; }
    public float TEnd { get; set; }
    public int Points { get; set; }
    public float Amplitude { get; set; }
    public float Frequency { get; set; }
    public float Phase { get; set; }

    public SignalParameters(float tStart, float tEnd, int points, float amplitude, float frequency, float phase)
    {
        TStart = tStart;
        TEnd = tEnd;
        Points = points;
        Amplitude = amplitude;
        Frequency = frequency;
        Phase = phase;
    }

    public static GuiObjectFactory CreateFactory()
    {
        var arguments = new Dictionary<string, Type>
        {
            { "tStart", typeof(float) },
            { "tEnd", typeof(float) },
            { "points", typeof(int) },
            { "amplitude", typeof(float) },
            { "frequency", typeof(float) },
            { "phase", typeof(float) }
        };

        var factory = new GuiObjectFactory(typeof(SignalParameters), arguments, "Signal Parameters");
        
        // Set default values
        factory.InstanceArguments["tStart"] = 0.0f;
        factory.InstanceArguments["tEnd"] = 1.0f;
        factory.InstanceArguments["points"] = 100;
        factory.InstanceArguments["amplitude"] = 1.0f;
        factory.InstanceArguments["frequency"] = 1.0f;
        factory.InstanceArguments["phase"] = 0.0f;

        return factory;
    }
}