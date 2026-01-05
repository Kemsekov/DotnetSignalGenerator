//TODO: спроси у чатагпт как тут лучше написать тесты
using System.Numerics;
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


public class AddNormalNoiseFilter : IFilter
{
    private readonly float _mean;
    private readonly float _std;
    private readonly np.random _rand;

    public AddNormalNoiseFilter(float mean = 0, float std = 1, int seed = -1)
    {
        if (std < 0)
            throw new ArgumentException("Standard deviation must be non-negative");

        _mean = mean;
        _std = std;
        _rand = new np.random();
        if (seed >= 0)
            _rand.seed(seed);
    }

    public ndarray Compute(ndarray signal)
    {
        var noise = np.zeros_like(signal);

        noise += _rand.randn(signal.shape);

        var j = new Complex(0, 1).ToNdarray();
        if (signal.Dtype == np.Complex)
        {
            var to_add = _rand.randn(signal.shape).astype(np.Complex) * j;
            noise += to_add;
        }

        return signal + noise * _std + _mean;
    }
}

public class LowPassFilterMethod : IFilterMethod
{
    private readonly float _alpha;

    public LowPassFilterMethod(float alpha = 0.9f)
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentException("Alpha must be between 0 and 1 (exclusive)");
        _alpha = alpha;
    }

    /// <inheritdoc/>
    public float Step(float t, float xi, float xi_prev) => _alpha * (t - xi) + xi;
}

public class HighPassFilterMethod : IFilterMethod
{
    private readonly float _alpha;

    public HighPassFilterMethod(float alpha = 0.9f)
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentException("Alpha must be between 0 and 1 (exclusive)");
        _alpha = alpha;
    }

    /// <inheritdoc/>
    public float Step(float t, float xi, float xi_prev) => _alpha * (t + xi - xi_prev);
}
/// <summary>
/// Implements a first-order Low-Pass Filter (LPF) logic.
/// </summary>
public class LowPassFilter : ZeroPhaseFilter
{
    public LowPassFilter(float alpha = 0.9f) : base(new LowPassFilterMethod(alpha))
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentException("Alpha must be between 0 and 1 (exclusive)");
    }
}
/// <summary>
/// Implements a first-order High-Pass Filter (HPF) logic.
/// </summary>
public class HighPassFilter : ZeroPhaseFilter
{
    public HighPassFilter(float alpha = 0.9f) : base(new HighPassFilterMethod(alpha))
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentException("Alpha must be between 0 and 1 (exclusive)");
    }
}
/// <summary>
/// Implements a zero-phase filter by applying a filter method in both forward and backward directions.
/// </summary>
public class ZeroPhaseFilter : IFilter
{
    readonly IFilterMethod _filterMethod;

    public ZeroPhaseFilter(IFilterMethod filterMethod)
    {
        if (filterMethod == null)
            throw new ArgumentNullException(nameof(filterMethod));
        _filterMethod = filterMethod;
    }

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

public class CutOutliersFilter : IFilter
{
    private readonly float _lowQuantile;
    private readonly float _highQuantile;

    public CutOutliersFilter(float lowQuantile = 0.05f, float highQuantile = 0.95f)
    {
        if (lowQuantile < 0 || lowQuantile >= 1)
            throw new ArgumentException("Low quantile must be between 0 (inclusive) and 1 (exclusive)");
        if (highQuantile <= 0 || highQuantile > 1)
            throw new ArgumentException("High quantile must be between 0 (exclusive) and 1 (inclusive)");
        if (lowQuantile >= highQuantile)
            throw new ArgumentException("Low quantile must be less than high quantile");

        _lowQuantile = lowQuantile;
        _highQuantile = highQuantile;
    }

    public ndarray Compute(ndarray signal)
    {
        var l = np.quantile(signal, _lowQuantile);
        var h = np.quantile(signal, _highQuantile);
        signal = signal.Copy();
        signal[signal < l] = l;
        signal[signal > h] = h;
        return signal;
    }
}

public class BilateralFilter : IFilter
{
    public float SigmaS { get; }
    public float SigmaR { get; }
    public int WindowSize { get; }

    public BilateralFilter(float sigmaS=1, float sigmaR=1,int windowSize = -1)
    {
        SigmaS=sigmaS;
        SigmaR=sigmaR;
        if (SigmaS <= 0 || SigmaR<=0)
            throw new ArgumentException("SigmaS and SigmaR should be positive real");
        WindowSize = windowSize<0 ? (int)MathF.Ceiling(3f*sigmaS) : windowSize;
    }
    ndarray _gaussian(ndarray t,float sigma)
        => np.exp(-t*t/(2*sigma*sigma));
    public ndarray ComputeNaive(ndarray signal)
    {
        //We assume input signals of shape (N,)
        if(signal.shape.iDims.Length!=1)
            throw new ArgumentException("BilateralFilter signal should be one-dimensional");
        var p = np.arange(0,signal.shape[0],dtype:np.Int32).reshape(-1,1);
        var q = np.arange(0,signal.shape[0],dtype:np.Int32).reshape(1,-1);

        var Gs = _gaussian(np.absolute(p-q),SigmaS);
        var Gr = _gaussian(signal.A(p)-signal.A(q),SigmaR);
        var prod = Gs*Gr;
        var W = np.sum(prod,-1)+1e-7; //avoid div by zero

        var res = np.sum(prod*signal.A(q),-1)/W;

        return res;
    }

    public ndarray Compute(ndarray signal)
    {
        System.Console.WriteLine($"shape {signal.shape}");
        //We assume input signals of shape (N,)
        var origShape = signal.shape;
        signal = signal.reshape(-1);
            
        var p = np.arange(0,signal.shape[0],dtype:np.Int32).reshape(-1,1);
        var shift = np.arange(-WindowSize,WindowSize+1,dtype:np.Int32).reshape(1,-1);
        var q = p+shift;

        var validItemsMask = ~((q<0) | (q>=signal.shape[0]));
        validItemsMask=validItemsMask.astype(signal.Dtype);

        q[q<0]=0; //move away a lot
        q[q>=signal.shape[0]]=signal.shape[0]-1;
        var Gs = _gaussian(np.sqrt(np.power(p-q,2)),SigmaS);
        
        var Gr = _gaussian(signal.A(p)-signal.A(q),SigmaR);
        var prod = validItemsMask*Gs*Gr;
        var W = np.sum(prod,-1)+1e-7; //avoid div by zero

        var res = np.sum(prod*signal.A(q),-1)/W;
        
        return res.reshape(origShape);
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
