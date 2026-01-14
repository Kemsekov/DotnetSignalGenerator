using System;
using System.Collections.Generic;
using System.Linq;
using SignalCore.Storage;

namespace SignalCore.Parameters;

public class SignalParameters
{
    public int ComputePoints { get; set; }
    public int RenderPoints { get; set; }

    public SignalParameters(int computePoints = 1024, int renderPoints = 256)
    {
        if (computePoints <= 0 || renderPoints <= 0)
            throw new ArgumentException("computePoints and renderPoints must be positive numbers!");
        ComputePoints = computePoints;
        RenderPoints = renderPoints;
    }

    public static ObjectFactory CreateFactory()
    {
        var ctor =
            typeof(SignalParameters)
            .GetSupportedConstructor(ArgumentsTypesUtils.SupportedTypes)?
            .ToDictionary(v => v.Key, v => v.Value);
        return new ObjectFactory(typeof(SignalParameters), ctor ?? throw new Exception());
    }
}