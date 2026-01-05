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

    public static long numel(this ndarray x)
    {
        long prod = 1;
        checked
        {
            foreach (var d in x.shape.iDims)
                prod *= d;
        }
        return prod;
    }

    /// <summary>
    /// Changes 1d
    /// </summary>
    /// <param name="x"></param>
    /// <param name="newLength"></param>
    /// <returns></returns>
    public static ndarray resample(this ndarray x,long[] newShape,dtype? dtype = null)
    {
        var newLength = newShape.Aggregate((a,b)=>a*b);
        double r=0;
        var tNew = np.linspace(0,newLength,ref r, (int)newLength,dtype:dtype);
        var tOld = np.linspace(0,newLength,ref r, (int)x.numel(),dtype:dtype);
        var xNew = np.interp(tNew,tOld,x.reshape(-1));
        return xNew.reshape(new shape(newShape));
    }

    public static ndarray at(this ndarray array,params int[] ind)
    {
        var v = array[ind];
        return np.array(v,copy:false);
    }
    public static ndarray at(this ndarray array, ndarray ind)
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

