using NumSharp;
using MathNet.Numerics.Statistics;

namespace SignalCore;

/// <summary>
/// Defines a contract for signal processing filters that operate on <see cref="NDArray"/> data.
/// </summary>
public interface IFilter
{
    /// <summary>
    /// Applies a filtering algorithm to the input signal.
    /// </summary>
    /// <param name="signal">The input <see cref="NDArray"/> representing the time-series data.</param>
    /// <returns>A new <see cref="NDArray"/> containing the filtered signal.</returns>
    public NDArray Apply(NDArray signal);
}

/// <summary>
/// Defines a contract for a single-step filtering calculation.
/// </summary>
public interface IFilterMethod
{
    /// <summary>
    /// Calculates the next state of the filter based on current and previous inputs.
    /// </summary>
    /// <param name="t">The current accumulated filter state.</param>
    /// <param name="xi">The current input signal value.</param>
    /// <param name="xi_prev">The previous input signal value.</param>
    /// <returns>The updated filter state value.</returns>
    public float Step(float t, float xi, float xi_prev);
}

/// <summary>
/// Implements a first-order Low-Pass Filter (LPF) logic.
/// </summary>
public class LowPassFilterMethod : IFilterMethod
{
    private float alpha;

    /// <summary>
    /// Initializes a new instance of the <see cref="LowPassFilterMethod"/> class.
    /// </summary>
    /// <param name="alpha">The smoothing factor (typically between 0 and 1).</param>
    public LowPassFilterMethod(float alpha)
    {
        this.alpha = alpha;
    }

    /// <inheritdoc/>
    public float Step(float t, float xi, float xi_prev)
    {
        return alpha * (t - xi) + xi;
    }
}

/// <summary>
/// Implements a first-order High-Pass Filter (HPF) logic.
/// </summary>
public class HighPassFilterMethod : IFilterMethod
{
    private float alpha;

    /// <summary>
    /// Initializes a new instance of the <see cref="HighPassFilterMethod"/> class.
    /// </summary>
    /// <param name="alpha">The filter coefficient determining the cutoff frequency.</param>
    public HighPassFilterMethod(float alpha)
    {
        this.alpha = alpha;
    }

    /// <inheritdoc/>
    public float Step(float t, float xi, float xi_prev)
    {
        return alpha * (t + xi - xi_prev);
    }
}

/// <summary>
/// Implements a zero-phase filter by applying a filter method in both forward and backward directions.
/// </summary>
public class BidirectionalFilter : IFilter
{
    /// <summary>
    /// Gets the alpha coefficient used by the filter method.
    /// </summary>
    public float Alpha { get; }

    private IFilterMethod _filterMethod;

    /// <summary>
    /// Initializes a new instance of the <see cref="BidirectionalFilter"/> class with a specific filtering strategy.
    /// </summary>
    /// <param name="filterMethod">The <see cref="IFilterMethod"/> strategy (e.g., LowPass or HighPass) to apply.</param>
    public BidirectionalFilter(IFilterMethod filterMethod)
    {
        _filterMethod = filterMethod;
    }

    /// <summary>
    /// Applies the filter forward and then backward to eliminate phase shift.
    /// </summary>
    /// <param name="signal">The signal to be filtered.</param>
    /// <returns>A phase-corrected <see cref="NDArray"/> signal.</returns>
    public NDArray Apply(NDArray signal)
    {
        var n = signal.shape[0];
        if (n == 0)
            return signal.copy();

        // Forward Pass
        var s1 = Ema(signal);

        // Backward Pass: reverse the result, filter again, and reverse back
        var reversed = s1["::-1"];
        var s = Ema(reversed)["::-1"];

        // Final result scaled to maintain amplitude

        return s;
    }

    /// <summary>
    /// Computes an Exponential Moving Average (or similar step-based filter) across the array.
    /// </summary>
    /// <param name="x">The input array.</param>
    /// <returns>The processed array.</returns>
    private NDArray Ema(NDArray x)
    {
        var n = x.shape[0];
        var result = np.zeros_like(x);

        float t = x[0];
        result[0] = t;

        for (int i = 1; i < n; i++)
        {
            float xi = x[i];
            float prev_xi = i - 1 < 0 ? xi : x[i - 1];
            result[i] = t = _filterMethod.Step(t, xi, prev_xi);
        }
        return result;
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
    /// <param name="originalSignal">The reference <see cref="NDArray"/> (e.g., raw data).</param>
    /// <param name="newSignal">The <see cref="NDArray"/> to be rescaled (e.g., filtered data).</param>
    /// <param name="lowerQ">The lower quantile threshold (default 0.05).</param>
    /// <param name="highQ">The upper quantile threshold (default 0.95).</param>
    /// <returns>A scale factor calculated based on the ratio of quantiles between the two signals.</returns>
    /// <remarks>
    /// Note: This method modifies <paramref name="newSignal"/> in-place for the Z-score normalization portion 
    /// before calculating the final quantile-based scale factor.
    /// </remarks>
    public static float RescaleSignal(NDArray originalSignal, NDArray newSignal, float lowerQ = 0.05f, float highQ = 0.95f)
    {
        var meanX = originalSignal.mean();
        var stdX = originalSignal.std();

        var meanS = newSignal.mean();
        var stdS = newSignal.std();

        // Standardize newSignal and project onto originalSignal's mean/std
        newSignal = (newSignal - meanS) / stdS * stdX + meanX;

        // Calculate Quantiles for both signals
        var q95 = newSignal.quantile(highQ);
        var q05 = newSignal.quantile(lowerQ);

        var xq95 = originalSignal.quantile(highQ);
        var xq05 = originalSignal.quantile(lowerQ);

        // Average the ratio of the upper and lower quantile spreads to determine final scale
        var scale = (xq95 / q95 + xq05 / q05) / 2.0;
        return (float)scale;
    }
}
