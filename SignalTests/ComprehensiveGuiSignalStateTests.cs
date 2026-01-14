using System;
using System.Linq;
using SignalCore;
using SignalCore.GuiState;
using SignalCore.Storage;
using SignalCore.Parameters;
using Xunit;

namespace SignalTests;

public class ComprehensiveGuiSignalStateTests
{
    [Fact]
    public void TestGuiSignalStateWithSimpleSourcesAndFilters()
    {
        using var storage = new SignalStorage(":memory:");
        
        // Create signal generators
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var squareGen = new SquareGenerator(amplitude: 0.5f, frequency: 2);
        
        // Create ObjectFactory instances for the generators
        var genFactory1 = new ObjectFactory(sinusoidGen, []);
        var genFactory2 = new ObjectFactory(squareGen, []);
        
        // Create a ComputeSignal with simple operations
        var generators = new[]
        {
            ("A", (ISignalGenerator)sinusoidGen),
            ("B", (ISignalGenerator)squareGen)
        };
        
        var ops = new ISignalOperation[]
        {
            new LowPassFilter(0.8f),
            new AddNormalNoiseFilter(mean: 0, std: 0.1f)
        };
        
        var statistics = new ISignalStatistic[]
        {
            new MeanStatistic(),
            new StdStatistic()
        };
        
        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A+B",
            ops,
            statistics
        );
        
        computeSignal.Run();
        computeSignal.Wait();
        
        // Create GuiSignalState
        var guiState = new GuiSignalState(
            objectName: "SimpleTest",
            computedSignal: computeSignal.ComputedSignal,
            signalStatistics: computeSignal.ComputedSignal.Stats?.Select(s => (s.name, s.stat)).ToArray(),
            completedPercent: 100,
            sources: new[] { genFactory1, genFactory2 },
            expression: "A+B",
            filters: new[]
            {
                (true, new ObjectFactory(new LowPassFilter(0.8f), [])),
                (false, new ObjectFactory(new AddNormalNoiseFilter(0, 0.1f), []))  // not visible
            },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 100), ("renderPoints", 256)]
            )
        );
        
        // Save and load
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
        storage.AddSessionState(sessionStateModel);
        
        var loadedState = GuiStateConverter.LoadFromDB(storage, "SimpleTest");
        
        // Verify loaded state matches original
        Assert.Equal(guiState.ObjectName, loadedState.ObjectName);
        Assert.Equal(guiState.Expression, loadedState.Expression);
        Assert.Equal(guiState.CompletedPercent, loadedState.CompletedPercent);
        
        // Verify signal data integrity
        if (guiState.ComputedSignal?.X != null && loadedState.ComputedSignal?.X != null)
        {
            Assert.Equal(guiState.ComputedSignal.X.Length, loadedState.ComputedSignal.X.Length);
        }
        
        if (guiState.ComputedSignal?.Y != null && loadedState.ComputedSignal?.Y != null)
        {
            Assert.Equal(guiState.ComputedSignal.Y.Length, loadedState.ComputedSignal.Y.Length);
        }
        
        // Verify sources count
        Assert.Equal(guiState.Sources.Count(), loadedState.Sources.Count());
        
        // Verify filters count
        Assert.Equal(guiState.Filters.Count(), loadedState.Filters.Count());
    }
    
    [Fact]
    public void TestGuiSignalStateWithAllOperationTypes()
    {
        using var storage = new SignalStorage(":memory:");
        
        // Create a signal generator
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var genFactory = new ObjectFactory(sinusoidGen, []);
        
        // Create operations of different types
        var lowPassFilter = new LowPassFilter(0.9f);
        var bilateralFilter = new BilateralFilter(sigmaS: 1, sigmaR: 1);
        var zScoreNorm = new ZScoreNormalization(0, 1);
        var minMaxNorm = new MinMaxNormalization(0, 1);
        var fftTransform = new FFTTransform();
        
        // Create ObjectFactory instances for operations
        var filterFactory1 = new ObjectFactory(lowPassFilter, []);
        var filterFactory2 = new ObjectFactory(bilateralFilter, []);
        var normFactory1 = new ObjectFactory(zScoreNorm, []);
        var normFactory2 = new ObjectFactory(minMaxNorm, []);
        var transformFactory = new ObjectFactory(fftTransform, []);
        
        // Create ComputeSignal
        var generators = new[] { ("A", (ISignalGenerator)sinusoidGen) };
        var ops = new ISignalOperation[] { lowPassFilter, zScoreNorm, fftTransform };
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
        
        // Create GuiSignalState with all operation types
        var guiState = new GuiSignalState(
            objectName: "AllOpsTest",
            computedSignal: computeSignal.ComputedSignal,
            sources: new[] { genFactory },
            expression: "A",
            filters: new[]
            {
                (true, filterFactory1),   // visible filter
                (false, filterFactory2),  // invisible filter
                (true, normFactory1),     // visible normalization
                (false, normFactory2),    // invisible normalization
                (true, transformFactory)  // visible transform
            },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 50), ("renderPoints", 256)]
            )
        );
        
        // Save and load
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
        storage.AddSessionState(sessionStateModel);
        
        var loadedState = GuiStateConverter.LoadFromDB(storage, "AllOpsTest");
        
        // Verify loaded state
        Assert.Equal(guiState.ObjectName, loadedState.ObjectName);
        Assert.Equal(guiState.Expression, loadedState.Expression);
        
        // Verify sources count
        Assert.Equal(guiState.Sources.Count(), loadedState.Sources.Count());
        
        // Verify filters count
        Assert.Equal(guiState.Filters.Count(), loadedState.Filters.Count());
    }
    
    [Fact]
    public void TestGuiSignalStateMultipleSaveLoadCycles()
    {
        using var storage = new SignalStorage(":memory:");
        
        // Create a simple signal generator
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var genFactory = new ObjectFactory(sinusoidGen, []);
        
        // Create ComputeSignal
        var generators = new[] { ("A", (ISignalGenerator)sinusoidGen) };
        var ops = new ISignalOperation[] { new LowPassFilter(0.8f) };
        var statistics = new ISignalStatistic[] { new MeanStatistic() };
        
        var computeSignal = new ComputeSignal(
            computePoints: 25,
            generators,
            "A",
            ops,
            statistics
        );
        
        computeSignal.Run();
        computeSignal.Wait();
        
        // Create initial state
        var guiState = new GuiSignalState(
            objectName: "CycleTest",
            computedSignal: computeSignal.ComputedSignal,
            sources: new[] { genFactory },
            expression: "A",
            filters: new[] { (true, new ObjectFactory(new LowPassFilter(0.8f), [])) },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 25), ("renderPoints", 256)]
            )
        );
        
        // Perform multiple save/load cycles
        for (int i = 0; i < 3; i++)
        {
            var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
            storage.AddSessionState(sessionStateModel);
            
            var loadedState = GuiStateConverter.LoadFromDB(storage, "CycleTest");
            
            // Verify consistency across cycles
            Assert.Equal(guiState.ObjectName, loadedState.ObjectName);
            Assert.Equal(guiState.Expression, loadedState.Expression);
            Assert.Equal(guiState.Sources.Count(), loadedState.Sources.Count());
            Assert.Equal(guiState.Filters.Count(), loadedState.Filters.Count());
            
            // Update guiState for next iteration
            guiState = loadedState;
        }
    }
    
    [Fact]
    public void TestGuiSignalStateClone()
    {
        // Test the clone functionality
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var genFactory = new ObjectFactory(sinusoidGen, []);
        
        // Create ComputeSignal
        var generators = new[] { ("A", (ISignalGenerator)sinusoidGen) };
        var ops = new ISignalOperation[] { new LowPassFilter(0.8f) };
        var statistics = new ISignalStatistic[] { new MeanStatistic() };
        
        var computeSignal = new ComputeSignal(
            computePoints: 30,
            generators,
            "A",
            ops,
            statistics
        );
        
        computeSignal.Run();
        computeSignal.Wait();
        
        var originalState = new GuiSignalState(
            objectName: "CloneTest",
            computedSignal: computeSignal.ComputedSignal,
            sources: new[] { genFactory },
            expression: "A",
            filters: new[] { (true, new ObjectFactory(new LowPassFilter(0.8f), [])) },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 30), ("renderPoints", 256)]
            )
        );
        
        // Test cloning
        var clonedState = originalState.Clone();
        
        // Verify clone properties
        Assert.Equal(originalState.ObjectName, clonedState.ObjectName);
        Assert.Equal(originalState.Expression, clonedState.Expression);
        Assert.Equal(originalState.Sources.Count(), clonedState.Sources.Count());
        Assert.Equal(originalState.Filters.Count(), clonedState.Filters.Count());
        Assert.Equal(originalState.CompletedPercent, clonedState.CompletedPercent);
        
        // Verify that they are separate instances (modify one, check the other is unchanged)
        clonedState.ObjectName = "ModifiedClone";
        Assert.NotEqual(originalState.ObjectName, clonedState.ObjectName);
        Assert.Equal("CloneTest", originalState.ObjectName);
        Assert.Equal("ModifiedClone", clonedState.ObjectName);
    }
}