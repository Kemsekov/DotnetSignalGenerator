namespace SignalCore;


public class MeanStatistic : ISignalStatistic
{
    public string Name => "Mean";

    public ndarray Compute(ndarray signal)
    {
        return signal.Mean();
    }
}
public class StdStatistic : ISignalStatistic
{
    public string Name => "STD";

    public ndarray Compute(ndarray signal)
    {
        return signal.Std();
    }
}

public class Quantile25Statistic : ISignalStatistic
{
    public string Name => $"0.25 Quantile";
    public ndarray Compute(ndarray signal)
    {
        return np.quantile(signal,0.25);
    }
}
public class Quantile75Statistic : ISignalStatistic
{
    public string Name => $"0.75 Quantile";
    public ndarray Compute(ndarray signal)
    {
        return np.quantile(signal,0.75);
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
    public string Name => "Peak2Peak Amp";

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

public class MedianStatistic : ISignalStatistic
{
    public string Name => "Median";

    public ndarray Compute(ndarray signal)
    {
        return np.median(signal);
    }
}

public class MinStatistic : ISignalStatistic
{
    public string Name => "Minimum";

    public ndarray Compute(ndarray signal)
    {
        return np.min(signal);
    }
}

public class MaxStatistic : ISignalStatistic
{
    public string Name => "Maximum";

    public ndarray Compute(ndarray signal)
    {
        return np.max(signal);
    }
}

public class SkewnessStatistic : ISignalStatistic
{
    public string Name => "Skewness";

    public ndarray Compute(ndarray signal)
    {
        var mean = signal.Mean();
        var std = signal.Std();
        var centered = signal - mean;
        return np.power(centered,3).Mean() / np.power(std,3);
    }
}

public class KurtosisStatistic : ISignalStatistic
{
    public string Name => "Kurtosis";

    public ndarray Compute(ndarray signal)
    {
        var mean = signal.Mean();
        var std = signal.Std();
        var centered = signal - mean;
        return np.power(centered,4).Mean() / np.power(std,4) - 3; // Excess kurtosis
    }
}

public class EnergyStatistic : ISignalStatistic
{
    public string Name => "Energy";

    public ndarray Compute(ndarray signal)
    {
        return (signal * signal).Sum();
    }
}

public class PowerStatistic : ISignalStatistic
{
    public string Name => "Power";

    public ndarray Compute(ndarray signal)
    {
        return (signal * signal).Mean();
    }
}
