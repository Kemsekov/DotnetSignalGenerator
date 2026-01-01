// TODO: добавь тесты, валидатор данных какой-нибудь общий
namespace SignalCore;

/// <param name="tStart">Start time of signal</param>
/// <param name="tEnd">End time of signal</param>
/// <param name="points">Number of points of discretization</param>
/// <param name="amplitude">Signal amplitude</param>
/// <param name="frequency">Signal frequency</param>
/// <param name="phase">Signal phase</param>
public abstract class SignalGeneratorBase(float tStart=0, float tEnd=1, float amplitude=1, float frequency=1, float phase=0) : ISignalGenerator
{
    public readonly float TStart = tStart;
    public readonly float TEnd = tEnd;
    public readonly float Amplitude = amplitude;
    public readonly float Frequency = frequency;
    public readonly float Phase = phase;

    public abstract ndarray Sample(int points);
}

public class SinusoidGenerator : SignalGeneratorBase
{
    public SinusoidGenerator(float tStart=0, float tEnd=1, float amplitude=1, float frequency=1, float phase=0) : base(tStart, tEnd, amplitude, frequency, phase)
    {
    }

    public override ndarray Sample(int points)
    {
        double r = 0;
        var t = np.linspace(TStart,TEnd,ref r,points,dtype:np.Float32);
        var arg = 2*Math.PI*Frequency*t+Phase;
        var y = Amplitude*np.sin(arg);
        return np.stack([t,y],0);
    }
}

public class SquareGenerator : SignalGeneratorBase
{
    public SquareGenerator(float tStart=0, float tEnd=1, float amplitude=1, float frequency=1, float phase=0) : base(tStart, tEnd, amplitude, frequency, phase)
    {
    }

    public override ndarray Sample(int points)
    {
        double r = 0;
        var t = np.linspace(TStart,TEnd, ref r,points,dtype:np.Float32);
        var arg = 2*Math.PI*Frequency*t+Phase;
        var y = Amplitude*np.sign(np.sin(arg));
        return np.stack([t,y],0);
    }
}

public class TriangleGenerator : SignalGeneratorBase
{
    public TriangleGenerator(float tStart=0, float tEnd=1, float amplitude=1, float frequency=1, float phase=0) : base(tStart, tEnd, amplitude, frequency, phase)
    {
    }

    public override ndarray Sample(int points)
    {
        double r = 0;
        var t = np.linspace(TStart,TEnd,ref r,points,dtype:np.Float32);
        var arg = t * Frequency + Phase/(2*Math.PI);
        var y = Amplitude * (2 * np.absolute(2 * (arg - np.floor(arg + 0.5))) - 1);
        return np.stack([t,y],0);
    }
}
public class SawToothGenerator : SignalGeneratorBase
{
    public SawToothGenerator(float tStart=0, float tEnd=1, float amplitude=1, float frequency=1, float phase=0) : base(tStart, tEnd, amplitude, frequency, phase)
    {
    }

    public override ndarray Sample(int points)
    {
        double r = 0;
        var t = np.linspace(TStart,TEnd, ref r,points,dtype:np.Float32);
        var arg = t * Frequency + Phase/(2*Math.PI);
        var y = Amplitude * 2 * (arg - np.floor(arg + 0.5));
        return np.stack([t,y],0);
    }
}
