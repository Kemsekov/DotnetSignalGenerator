using NumSharp;
namespace SignalCore;


public interface ISignalGenerator
{
    NDArray Sample(float tStart, float tEnd,int points, float amplitude,float frequency, float phase);
}

public class Sinusoid : ISignalGenerator
{
    public NDArray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        var t = np.linspace(tStart,tEnd,points);
        var arg = 2*np.pi*frequency*t+phase;
        var y = amplitude*np.sin(arg);
        return np.stack([t,y],0);
    }
}

public class Square : ISignalGenerator
{
    public NDArray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        var t = np.linspace(tStart,tEnd,points);
        var arg = 2*np.pi*frequency*t+phase;
        var y = amplitude*np.sign(np.sin(arg));
        return np.stack([t,y],0);
    }
}

public class Triangle : ISignalGenerator
{
    public NDArray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        var t = np.linspace(tStart,tEnd,points);
        var arg = t * frequency + phase/(2*np.pi);
        var y = amplitude * (2 * np.abs(2 * (arg - np.floor(arg + 0.5))) - 1);
        return np.stack([t,y],0);
    }
}
public class SawTooth : ISignalGenerator
{
    public NDArray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        var t = np.linspace(tStart,tEnd,points);
        var arg = t * frequency + phase/(2*np.pi);
        var y = amplitude * 2 * (arg - np.floor(arg + 0.5));
        return np.stack([t,y],0);
    }
}
