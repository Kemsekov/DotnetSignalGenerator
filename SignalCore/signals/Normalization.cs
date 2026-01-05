namespace SignalCore;

public class MinMaxNormalization : INormalization
{
    public MinMaxNormalization(float min=0,float max = 1)
    {
        this.min = min;
        this.max = max;
        scale = max-min;
        if(min>=max)
            throw new ArgumentException("min parameter must be smaller than max");
    }
    readonly float scale;
    readonly float min;
    readonly float max;

    public ndarray Compute(ndarray signal)
    {
        if (signal.Dtype == np.Complex)
        {
            var magnitude = np.absolute(signal);
            var minMag = np.min(magnitude);
            var maxMag = np.max(magnitude);
            
            return (signal/maxMag*max);
        }
        var smin = np.min(signal);
        var smax = np.max(signal);
        return (signal-smin)/(smax-smin+1e-7)*scale+min;
    }
}

public class ZScoreNormalization : INormalization
{
    public ZScoreNormalization(float mean=0, float std = 1)
    {
        if(std<=0)
            throw new ArgumentException("std parameter must be positive");
        this.mean = mean;
        this.std = std;
    }
    readonly float mean;
    readonly float std;
    public ndarray Compute(ndarray signal)
    {
        var smean = signal.Mean();
        var sstd = signal.Std()+1e-7;
        return (signal-smean)/sstd*std+mean;
    }
}

