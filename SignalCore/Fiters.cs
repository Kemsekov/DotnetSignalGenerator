using NumSharp;
using MathNet.Numerics.Statistics;

namespace SignalCore;

public interface IFilter
{
    public NDArray Apply(NDArray signal);
}

public interface IFilterMethod
{
    public float Step(float t, float xi, float xi_prev);
}

public class LowPassFilterMethod : IFilterMethod
{
    private float alpha;

    public LowPassFilterMethod(float alpha)
    {
        this.alpha=alpha;
    }
    public float Step(float t, float xi, float xi_prev)
    {
        return alpha*(t-xi)+xi;
    }
}
public class HighPassFilterMethod : IFilterMethod
{
    private float alpha;

    public HighPassFilterMethod(float alpha)
    {
        this.alpha=alpha;
    }
    public float Step(float t, float xi, float xi_prev)
    {
        return alpha*(t+xi-xi_prev);
    }
}

public class BidirectionalFilter : IFilter
{
    public float Alpha { get; }

    private IFilterMethod _filterMethod;

    public BidirectionalFilter(IFilterMethod filterMethod)
    {
        _filterMethod=filterMethod;
    }

    public NDArray Apply(NDArray signal)
    {
        var n = signal.shape[0];
        if (n == 0)
            return signal.copy();

        // Forward EMA
        var s1 = Ema(signal);

        // Backward EMA
        var reversed = s1["::- 1"];
        var s2 = Ema(reversed)["::- 1"];

        // s = s1 + s2
        var s = s2;
 
        double scale=0.5;

        return s * scale;
    }
    private NDArray Ema(NDArray x)
    {
        var n = x.shape[0];
        var result = np.zeros_like(x);

        var t = (float)x[0];
        result[0] = t;


        for (int i = 1; i < n; i++)
        {
            float xi = (float)x[i];
            float prev_xi = (float)(i -1<0 ? xi : x[i-1]);
            result[i] = t = _filterMethod.Step(t,xi,prev_xi);
        }
        return result;
    }

}

public static class SignalUtils
{
    public static float RescaleSignal(NDArray originalSignal, NDArray newSignal,float lowerQ=0.05f, float highQ=0.95f)
    {
        var meanX = originalSignal.mean();
        var stdX = originalSignal.std();

        var meanS = newSignal.mean();
        var stdS =  newSignal.std();

        newSignal = (newSignal - meanS) / stdS * stdX + meanX;

        // Quantile-based scaling
        var q95 = newSignal.quantile(highQ);
        var q05 = newSignal.quantile(lowerQ);

        var xq95 = originalSignal.quantile(highQ);
        var xq05 = originalSignal.quantile(lowerQ);

        var scale = (xq95 / q95 + xq05 / q05) / 2.0;
        return (float)scale;
    }
}