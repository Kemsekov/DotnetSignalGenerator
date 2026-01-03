//TODO: Add tests for transforms and their inverses
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NumpyDotNet;
using NWaves.Transforms;
using NWaves.Transforms.Wavelets;
namespace SignalCore;

public class FFTTransform : ITransform
{
    public ndarray Compute(ndarray signal)
    {
        var sig = signal.AsComplexArray().ToArray();
        Fourier.Forward(sig, FourierOptions.Matlab); 
        return np.array(sig,copy:false);
    }
}

public class InverseFFTTransform : ITransform
{
    public ndarray Compute(ndarray signal)
    {
        var sig = signal.AsComplexArray().ToArray();
        Fourier.Inverse(sig, FourierOptions.Matlab);
        return np.array(sig,copy:false);
    }
}
/// <summary>
/// Short-Time Fourier transform. Returns data in shape [N,K], where each N is time frame, K is spectra frequency
/// </summary>
public class STFTTransform(int fftSize = 1024, int hopSize = 512) : ITransform
{
    private readonly Stft _stft = new(fftSize, hopSize);

    public ndarray Compute(ndarray signal)
    {
        var x = signal.AsFloatArray();
        var spec = _stft.Direct(x);   // Complex[][]
        var res = spec.Select(
            v=>
            v.Item1.Zip(v.Item2)
            .Select(
                pair=>
                new Complex(pair.First,pair.Second)
            ).ToArray()
        ).ToArray();
        return np.stack(res);
    }
}

public class InverseSTFTTransform(int fftSize = 1024, int hopSize = 512) : ITransform
{
    private readonly Stft _stft = new(fftSize, hopSize);

    public ndarray Compute(ndarray signal)
    {
        var spec = signal.Select(v =>
        {
            var c = v.ToNdarray();
            return (c.Real.AsFloatArray(),c.Imag.AsFloatArray());
        }).ToList();

        var x = _stft.Inverse(spec);
        var res = np.array(x, copy: false);
        return res[~np.isnan(res)].ToNdarray();
    }
}

/// <summary>
/// Fast wavelet transform
/// </summary>
public class FWTTransform(string waveletName = "db4", int levels = 3) : ITransform
{
    public ndarray Compute(ndarray signal)
    {
        var input = signal.AsFloatArray();
        var fwt = new Fwt(input.Length, new Wavelet(waveletName));
        var output = new float[input.Length];
        fwt.Direct(input, output, levels);
        return np.array(output,copy:false);
    }
}

/// <summary>
/// Fast wavelet transform inverse
/// </summary>
public class InverseFWTTransform(string waveletName = "db4", int levels = 3) : ITransform
{
    public ndarray Compute(ndarray signal)
    {
        var input = signal.AsFloatArray();
        var fwt = new Fwt(input.Length, new Wavelet(waveletName));
        var output = new float[input.Length];
        fwt.Inverse(input, output, levels);
        return np.array(output,copy:false);
    }
}
