namespace SignalCore;

/// <summary>
/// Signal creation interface.
/// </summary>
public interface ISignalGenerator
{
    /// <summary>
    /// Samples signal with given properties
    /// </summary>
    /// <param name="tStart">Start time of signal</param>
    /// <param name="tEnd">End time of signal</param>
    /// <param name="points">Number of points of discretization</param>
    /// <param name="amplitude">Signal amplitude</param>
    /// <param name="frequency">Signal frequency</param>
    /// <param name="phase">Signal phase</param>
    /// <returns>Sample ndarray of shape [2,points] for time and signal values</returns>
    ndarray Sample(float tStart, float tEnd,int points, float amplitude,float frequency, float phase);
}

/// <summary>
/// Common interface that defines operation performed on signal
/// </summary>
public interface ISignalOperation
{
    public ndarray Compute(ndarray signal);
    
}

/// <summary>
/// Defines a contract for signal processing filters that operate on <see cref="ndarray"/> data.
/// Filters is such operation that only modifies existing signal in some way or another.
/// </summary>
public interface IFilter : ISignalOperation
{
    /// <summary>
    /// Applies a filtering algorithm to the input signal.
    /// </summary>
    /// <param name="signal">The input <see cref="ndarray"/> representing the time-series data.</param>
    /// <returns>A new <see cref="ndarray"/> containing the filtered signal.</returns>
    new ndarray Compute(ndarray signal);
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
/// Normalization of signal
/// </summary>
public interface INormalization : ISignalOperation
{
    new ndarray Compute(ndarray signal);
}

/// <summary>
/// Transform signal from time to frequency or time-frequency or any other domain. <br/>
/// Operations like FFT, Gabor transform, Wigner-Ville Distribution (WVD), Hilbert-Huang Transform (HHT), Stockwell Transform (S-Transform), etc
/// </summary>
public interface ITransform : ISignalOperation
{
    new ndarray Compute(ndarray signal);
}

/// <summary>
/// Signal statistic
/// </summary> <summary>
public interface ISignalStatistic : ISignalOperation
{
    string Name{get;}
    new ndarray Compute(ndarray signal);
}
