using System.Runtime.CompilerServices;
using MathNet.Numerics.Statistics;
using NumpyDotNet;

namespace SignalCore;
public static class NDArrayExtensions
{
    public static ndarray randn_(this ndarray arr)
    {
        var r = new np.random();
        arr[":"]=r.randn(arr.shape).astype(arr.Dtype);
        return arr;
    }
    public static ndarray at(this ndarray array,params int[] ind)
    {
        var v = array[ind];
        return np.array(v,copy:false);
    }
    public static ndarray at(this ndarray array,string ind)
    {
        var v = array[ind];
        return np.array(v,copy:false);
    }
    public static ndarray ToNdarray(this object? obj)=>np.array(obj,copy:false);
    public static float single(this object? obj) => obj.ToNdarray().AsFloatArray()[0];
}

