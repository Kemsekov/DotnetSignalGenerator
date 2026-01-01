// TODO: добавь тесты, валидатор данных какой-нибудь общий
using NumpyDotNet;
namespace SignalCore;
public interface ISignalGenerator
{
    ndarray Sample(float tStart, float tEnd,int points, float amplitude,float frequency, float phase);
}

public class SinusoidGenerator : ISignalGenerator
{
    public ndarray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        double r = 0;
        var t = np.linspace(tStart,tEnd,ref r,points,dtype:np.Float32);
        var arg = 2*Math.PI*frequency*t+phase;
        var y = amplitude*np.sin(arg);
        return np.stack([t,y],0);
    }
}

public class SquareGenerator : ISignalGenerator
{
    public ndarray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        double r = 0;
        var t = np.linspace(tStart,tEnd, ref r,points,dtype:np.Float32);
        var arg = 2*Math.PI*frequency*t+phase;
        var y = amplitude*np.sign(np.sin(arg));
        return np.stack([t,y],0);
    }
}

public class TriangleGenerator : ISignalGenerator
{
    public ndarray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        double r = 0;
        var t = np.linspace(tStart,tEnd,ref r,points,dtype:np.Float32);
        var arg = t * frequency + phase/(2*Math.PI);
        var y = amplitude * (2 * np.absolute(2 * (arg - np.floor(arg + 0.5))) - 1);
        return np.stack([t,y],0);
    }
}
public class SawToothGenerator : ISignalGenerator
{
    public ndarray Sample(float tStart, float tEnd,int points, float amplitude, float frequency, float phase)
    {
        double r = 0;
        var t = np.linspace(tStart,tEnd, ref r,points,dtype:np.Float32);
        var arg = t * frequency + phase/(2*Math.PI);
        var y = amplitude * 2 * (arg - np.floor(arg + 0.5));
        return np.stack([t,y],0);
    }
}
