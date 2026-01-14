using System;
using System.Linq;
using SignalCore;
using SignalCore.GuiState;
using SignalCore.Storage;
using SignalCore.Parameters;
using Xunit;

namespace SignalTests;

public class SignalOperationTest
{
    [Fact]
    public void TestSingleOperation()
    {
        // Create an in-memory database for testing
        using var storage = new SignalStorage(":memory:");
        
        // Create a simple ComputeSignal to generate actual signal data
        var generators = new[]
        {
            ("A", (ISignalGenerator)new SinusoidGenerator(amplitude: 1, frequency: 1))
        };
        
        // Test with just one operation first
        var ops = new ISignalOperation[] { new LowPassFilter(0.8f) };
        var statistics = new ISignalStatistic[] { new MeanStatistic() };
        
        var computeSignal = new ComputeSignal(
            computePoints: 50,
            generators,
            "A",
            ops,
            statistics
        );
        
        computeSignal.Run();
        computeSignal.Wait();
        
        Console.WriteLine($"Single operation - X length: {computeSignal.ComputedSignal.X?.Length}, Y length: {computeSignal.ComputedSignal.Y?.Length}");
        
        Assert.NotNull(computeSignal.ComputedSignal.X);
        Assert.NotNull(computeSignal.ComputedSignal.Y);
        Assert.Equal(50, computeSignal.ComputedSignal.X.Length);
        Assert.Equal(50, computeSignal.ComputedSignal.Y.Length);
    }
    
    [Fact]
    public void TestMultipleOperations()
    {
        // Create an in-memory database for testing
        using var storage = new SignalStorage(":memory:");
        
        // Create a simple ComputeSignal to generate actual signal data
        var generators = new[]
        {
            ("A", (ISignalGenerator)new SinusoidGenerator(amplitude: 1, frequency: 1)),
            ("B", (ISignalGenerator)new SquareGenerator(amplitude: 0.5f, frequency: 2))
        };
        
        // Test with multiple operations
        var ops = new ISignalOperation[]
        {
            new LowPassFilter(0.8f),
            new HighPassFilter(0.7f),
            new ZScoreNormalization(0, 1)
        };
        var statistics = new ISignalStatistic[] { new MeanStatistic(), new StdStatistic() };
        
        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A+B",
            ops,
            statistics
        );
        
        computeSignal.Run();
        computeSignal.Wait();
        
        Console.WriteLine($"Multiple operations - X length: {computeSignal.ComputedSignal.X?.Length}, Y length: {computeSignal.ComputedSignal.Y?.Length}");
        
        Assert.NotNull(computeSignal.ComputedSignal.X);
        Assert.NotNull(computeSignal.ComputedSignal.Y);
        Assert.Equal(100, computeSignal.ComputedSignal.X.Length);
        Assert.Equal(100, computeSignal.ComputedSignal.Y.Length);
    }
}