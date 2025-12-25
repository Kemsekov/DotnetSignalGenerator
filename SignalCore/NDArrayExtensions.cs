using MathNet.Numerics.Statistics;
using NumSharp;

namespace SignalCore;
public static class NDArrayExtensions
{
    public static float quantile(this NDArray array,float q)
    {
        return ArrayStatistics.QuantileInplace(array.ToArray<float>(),q);
    }
    public static double quantile(this NDArray array,double q)
    {
        return ArrayStatistics.QuantileInplace(array.ToArray<double>(),q);
    }
}