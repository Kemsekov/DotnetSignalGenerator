//TODO: спроси у чатагпт как тут лучше написать тесты
using NumpyDotNet;
namespace SignalCore;

public static class Filters
{

    public static LowPassFilter LowPass(float alpha)
    {
        return new LowPassFilter(alpha);
    }
    public static HighPassFilter HighPass(float alpha)
    {
        return new HighPassFilter(alpha);
    }
    public static AddNormalNoiseFilter AddNormalNoise(float mean, float std)
    {
        return new AddNormalNoiseFilter(mean,std);
    }
    public static CutOutliersFilter CutOutliers(float lowQuantile,float highQuantile)
    {
        return new CutOutliersFilter(lowQuantile,highQuantile);;
    }
}





public class AddNormalNoiseFilter(float mean, float std) : IFilter
{
    public ndarray Compute(ndarray signal)
    {
        var rand = new np.random();
        return signal+rand.randn(signal.shape)*std+mean;
    }
}

public class LowPassFilterMethod(float alpha) : IFilterMethod
{
    /// <inheritdoc/>
    public float Step(float t, float xi, float xi_prev)=>alpha * (t - xi) + xi;
}

public class HighPassFilterMethod(float alpha) : IFilterMethod
{
    /// <inheritdoc/>
    public float Step(float t, float xi, float xi_prev)=>alpha * (t + xi - xi_prev);
}
/// <summary>
/// Implements a first-order Low-Pass Filter (LPF) logic.
/// </summary>
public class LowPassFilter : ZeroPhaseFilter
{
    public LowPassFilter(float alpha) : base(new LowPassFilterMethod(alpha)){}
}
/// <summary>
/// Implements a first-order High-Pass Filter (HPF) logic.
/// </summary>
public class HighPassFilter : ZeroPhaseFilter
{
    public HighPassFilter(float alpha) : base(new HighPassFilterMethod(alpha)){}
}
/// <summary>
/// Implements a zero-phase filter by applying a filter method in both forward and backward directions.
/// </summary>
public class ZeroPhaseFilter(IFilterMethod filterMethod) : IFilter
{
    readonly IFilterMethod _filterMethod=filterMethod;

    /// <summary>
    /// Applies the filter forward and then backward to eliminate phase shift.
    /// </summary>
    /// <param name="signal">The signal to be filtered.</param>
    /// <returns>A phase-corrected <see cref="ndarray"/> signal.</returns>
    public ndarray Compute(ndarray signal)
    {
        var n = signal.shape[0];
        if (n == 0)
            return np.copy(signal);

        // Forward Pass
        var s1 = Ema(signal);

        // Backward Pass: reverse the result, filter again, and reverse back
        var reversed = s1.at("::-1");
        var s = Ema(reversed).at("::-1");

        // Final result scaled to maintain amplitude

        return s;
    }

    /// <summary>
    /// Computes an Exponential Moving Average (or similar step-based filter) across the array.
    /// </summary>
    /// <param name="x">The input array.</param>
    /// <returns>The processed array.</returns>

    ndarray Ema(ndarray x1)
    {
        if (x1.Dtype == np.Complex)
        {
            var x = x1.AsComplexArray();
            var result = x.ToArray();
            var n = x.Length;
            var t = x[0];
            result[0] = t;

            for (int i = 1; i < n; i++)
            {
                var xi = x[i];
                var prev_xi = i - 1 < 0 ? xi : x[i - 1];
                var new_real = _filterMethod.Step((float)t.Real, (float)xi.Real, (float)prev_xi.Real);
                var new_imag = _filterMethod.Step((float)t.Imaginary, (float)xi.Imaginary, (float)prev_xi.Imaginary);
                t = new System.Numerics.Complex(new_real,new_imag);
                result[i] = t;
            }
            return np.array(result,copy:false);
        }
        else{
            var x = x1.AsFloatArray();
            var result = x.ToArray();
            var n = x.Length;
            var t = x[0];
            result[0] = t;

            for (int i = 1; i < n; i++)
            {
                var xi = x[i];
                var prev_xi = i - 1 < 0 ? xi : x[i - 1];
                result[i] = t = _filterMethod.Step(t, xi, prev_xi);
            }
            return np.array(result,copy:false);
        }
    }
}

// TODO: add check for 0<lowQ<highQ<1
public class CutOutliersFilter(float lowQuantile, float highQuantile) : IFilter
{
    public ndarray Compute(ndarray signal)
    {
        var l = np.quantile(signal,lowQuantile);
        var h = np.quantile(signal,highQuantile);
        signal = signal.Copy();
        signal[signal<l]=l;
        signal[signal>h]=h;
        return signal;
    }
}

/// <summary>
/// Provides utility functions for signal manipulation and statistical normalization.
/// </summary>
public static class SignalUtils
{
    /// <summary>
    /// Rescales a processed signal to match the statistical distribution (mean, standard deviation, and quantiles) of an original reference signal.
    /// </summary>
    public static ndarray RescaleSignal(ndarray originalSignal, ndarray newSignal, float lowerQ = 0.05f, float highQ = 0.95f)
    {
        var meanX = originalSignal.Mean();
        var stdX = originalSignal.Std();

        var meanS = newSignal.Mean();
        var stdS = newSignal.Std();

        // Standardize newSignal and project onto originalSignal's mean/std
        newSignal = (newSignal - meanS) / stdS * stdX + meanX;

        // Calculate Quantiles for both signals
        var q95 = np.quantile(newSignal,highQ);
        var q05 = np.quantile(newSignal,lowerQ);

        var xq95 = np.quantile(originalSignal,highQ);
        var xq05 = np.quantile(originalSignal,lowerQ);

        // Average the ratio of the upper and lower quantile spreads to determine final scale
        var scale = (xq95 / q95 + xq05 / q05) / 2.0;
        return newSignal*scale;
    }
}
