namespace SignalCore;

public class MinMaxNormalization(float min=0,float max=1) : INormalization
{
    readonly float scale = max-min;
    public ndarray Compute(ndarray signal)
    {
        var smin = np.min(signal);
        var smax = np.max(signal);
        return (signal-smin)/(smax-smin+1e-6)*scale+min;
    }
}

public class ZScoreNormalization(float mean=0, float std=1) : INormalization
{
    public ndarray Compute(ndarray signal)
    {
        var smean = signal.Mean();
        var sstd = signal.Std()+1e-6;
        return (signal-smean)/sstd*std+mean;
    }
}

