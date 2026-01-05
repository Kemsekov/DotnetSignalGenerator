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
public class STFTTransform : ITransform
{
    private Stft _stft;

    public STFTTransform(int fftSize = 16, int hopSize = 16)
    {
        Validate(fftSize, hopSize);
        _stft = new(fftSize, hopSize);
    }

    public static void Validate(int fftSize, int hopSize)
    {
        if (fftSize < 1 || hopSize < 1)
            throw new ArgumentException("fftSize and hopSize cannot be smaller than 1");
    }

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

public class InverseSTFTTransform : ITransform
{
    public InverseSTFTTransform(int fftSize = 16, int hopSize = 16)
    {
        STFTTransform.Validate(fftSize,hopSize);
        _stft = new(fftSize, hopSize);
    }
    private readonly Stft _stft;

    public ndarray Compute(ndarray signal)
    {
        var spec = signal.Select(v =>
        {
            var c = v.ToNdarray();
            return (c.Real.AsFloatArray(),c.Imag.AsFloatArray());
        }).ToList();

        var res = np.array(_stft.Inverse(spec), copy: false);
        var nanMask = np.isnan(res);

        return res.A(~nanMask);
    }
}

/// <summary>
/// Fast wavelet transform
/// </summary>
public class FWTTransform : ITransform
{
    private string waveletName;
    private int levels;

    public FWTTransform(string waveletName = "haar", int levels = 3)
    {
        //"haar", "db1".."db20", "sym2".."sym20", "coif1".."coif5"

        Validate(waveletName, levels);

        this.waveletName = waveletName;
        this.levels = levels;
    }

    public static void Validate(string waveletName, int levels)
    {
        var validWavelets = new string[][]
        {
            ["haar"],
            Enumerable.Range(1,20).Select(v=>$"db{v}").ToArray(),
            Enumerable.Range(2,20).Select(v=>$"sym{v}").ToArray(),
            Enumerable.Range(1,5).Select(v=>$"coif{v}").ToArray(),
        }.SelectMany(v => v).ToArray();

        if (!validWavelets.Contains(waveletName))
            throw new ArgumentException("Unknown waveletName.\nwaveletName must be one of\nhaar, db1..db20, sym2..sym20, coif1..coif5");

        if (levels < 1)
            throw new ArgumentException("levels must be positive");
    }

    public ndarray Compute(ndarray signal)
    {
        var input = signal.AsFloatArray();
        var fwt = new Fwt(input.Length, new Wavelet(waveletName));
        var output = new float[input.Length];
        fwt.Direct(input, output, levels);
        return np.array(output,copy:false).resample([levels,input.Length/levels]);
    }
}

/// <summary>
/// Fast wavelet transform inverse
/// </summary>
public class InverseFWTTransform : ITransform
{
    private string waveletName;
    private int levels;
    public InverseFWTTransform(string waveletName = "haar", int levels = 3)
    {
        FWTTransform.Validate(waveletName,levels);
        this.waveletName = waveletName;
        this.levels = levels;
    }
    public ndarray Compute(ndarray signal)
    {
        var input = signal.AsFloatArray();
        var fwt = new Fwt(input.Length, new Wavelet(waveletName));
        var output = new float[input.Length];
        fwt.Inverse(input, output, levels);
        return np.array(output,copy:false);
    }
}
