using NumpyDotNet;

namespace SignalCore;
/// <summary>
/// Signal statistic
/// </summary> <summary>
public interface ISignalStatistic
{
    string Name{get;}
    ndarray Compute(ndarray signal);
}

public class MeanStatistic : ISignalStatistic
{
    public string Name => "Signal Mean";

    public ndarray Compute(ndarray signal)
    {
        return signal.Mean();
    }
}
public class StdStatistic : ISignalStatistic
{
    public string Name => "Signal Standard Deviation";

    public ndarray Compute(ndarray signal)
    {
        return signal.Std();
    }
}
public class QuantileStatistic(float q) : ISignalStatistic
{
    public string Name => $"Signal {q} quantile";

    public ndarray Compute(ndarray signal)
    {
        return np.quantile(signal,q);
    }
}

public class RootMeanSquareStatistic : ISignalStatistic
{
    public string Name => "RMS";

    public ndarray Compute(ndarray signal)
    {
        return np.sqrt((signal*signal).Mean());
    }
}

public class PeakToPeakAmplitude : ISignalStatistic
{
    public string Name => "Peak2Peak Amplitude";

    public ndarray Compute(ndarray signal)
    {
        return np.max(signal)-np.min(signal);
    }
}

public class CrestFactorStatistic : ISignalStatistic
{
    public string Name => "Crest Factor";
    PeakToPeakAmplitude amp = new();
    RootMeanSquareStatistic rms = new();
    public ndarray Compute(ndarray signal)
    {
        return amp.Compute(signal)/rms.Compute(signal);
    }
}
