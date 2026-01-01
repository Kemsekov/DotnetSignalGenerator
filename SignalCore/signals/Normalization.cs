namespace SignalCore;

public class MinMaxNormalization(float min,float max) : INormalization
{
    readonly float scale = max-min;
    public ndarray Compute(ndarray signal)
    {
        var smin = np.min(signal);
        var smax = np.max(signal);
        return (signal-smin)/(smax-smin+1e-6)*scale+min;
    }
}

public class ZScoreNormalization(float mean, float std) : INormalization
{
    public ndarray Compute(ndarray signal)
    {
        var smean = signal.Mean();
        var sstd = signal.Std()+1e-6;
        return (signal-smean)/sstd*std+mean;
    }
}

