using System;
using System.Linq;
using SignalCore;
using SignalCore.GuiState;
using SignalCore.Storage;
using SignalCore.Parameters;
using Xunit;

namespace SignalTests;

public class GuiStateSaveLoadTest
{
    [Fact]
    public void TestGuiSignalStateSaveLoadRoundTrip()
    {
        // Create an in-memory database for testing
        using var storage = new SignalStorage(":memory:");
        
        // Create some signal generators
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var squareGen = new SquareGenerator(amplitude: 0.5f, frequency: 2);
        
        // Create ObjectFactory instances for the generators
        var genFactory1 = new ObjectFactory(sinusoidGen, []);
        var genFactory2 = new ObjectFactory(squareGen, []);
        
        // Create some signal operations (filters, transforms, normalizations)
        var lowPassFilter = new LowPassFilter(0.8f);
        var highPassFilter = new HighPassFilter(0.7f);
        var zScoreNorm = new ZScoreNormalization(0, 1);
        var fftTransform = new FFTTransform();
        
        // Create ObjectFactory instances for the operations
        var filterFactory1 = new ObjectFactory(lowPassFilter, []);
        var filterFactory2 = new ObjectFactory(highPassFilter, []);
        var normFactory = new ObjectFactory(zScoreNorm, []);
        var transformFactory = new ObjectFactory(fftTransform, []);
        
        // Create a ComputeSignal to generate actual signal data
        var generators = new[]
        {
            ("A", (ISignalGenerator)sinusoidGen),
            ("B", (ISignalGenerator)squareGen)
        };
        
        var ops = new ISignalOperation[]
        {
            lowPassFilter,
            highPassFilter,
            zScoreNorm
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

        // Verify that the original computed signal has data before saving
        Assert.NotNull(computeSignal.ComputedSignal.X);
        Assert.NotNull(computeSignal.ComputedSignal.Y);
        Assert.Equal(100, computeSignal.ComputedSignal.X.Length);
        Assert.Equal(100, computeSignal.ComputedSignal.Y.Length);

        // Create a GuiSignalState with all the components
        var guiState = new GuiSignalState(
            objectName: "TestState",
            computedSignal: computeSignal.ComputedSignal,
            signalStatistics: computeSignal.ComputedSignal.Stats?.Select(s => (s.name, s.stat)).ToArray(),
            completedPercent: 100,
            sources: new[] { genFactory1, genFactory2 },
            expression: "A+B",
            filters: new[]
            {
                (true, filterFactory1),  // visible filter
                (true, filterFactory2),  // visible filter
                (true, normFactory)      // visible normalization
            },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 100), ("renderPoints", 256)]
            )
        );
        
        // Save the state to database
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
        storage.AddSessionState(sessionStateModel);
        
        // Load the state from database
        var loadedState = GuiStateConverter.LoadFromDB(storage, "TestState");
        
        // Verify that the loaded state matches the original
        Assert.Equal(guiState.ObjectName, loadedState.ObjectName);
        Assert.Equal(guiState.Expression, loadedState.Expression);
        Assert.Equal(guiState.CompletedPercent, loadedState.CompletedPercent);
        
        // Compare computed signal data (with tolerance for floating point errors)
        if (guiState.ComputedSignal?.X != null && loadedState.ComputedSignal?.X != null)
        {
            Assert.Equal(guiState.ComputedSignal.X.Length, loadedState.ComputedSignal.X.Length);
            for (int i = 0; i < guiState.ComputedSignal.X.Length; i++)
            {
                Assert.Equal(guiState.ComputedSignal.X[i], loadedState.ComputedSignal.X[i], precision: 5);
            }
        }
        
        if (guiState.ComputedSignal?.Y != null && loadedState.ComputedSignal?.Y != null)
        {
            Assert.Equal(guiState.ComputedSignal.Y.Length, loadedState.ComputedSignal.Y.Length);
            for (int i = 0; i < guiState.ComputedSignal.Y.Length; i++)
            {
                Assert.Equal(guiState.ComputedSignal.Y[i], loadedState.ComputedSignal.Y[i], precision: 5);
            }
        }
        
        // Compare signal statistics
        if (guiState.SignalStatistics != null && loadedState.SignalStatistics != null)
        {
            Assert.Equal(guiState.SignalStatistics.Length, loadedState.SignalStatistics.Length);
            for (int i = 0; i < guiState.SignalStatistics.Length; i++)
            {
                Assert.Equal(guiState.SignalStatistics[i].name, loadedState.SignalStatistics[i].name);
                Assert.Equal(guiState.SignalStatistics[i].stat, loadedState.SignalStatistics[i].stat, precision: 5);
            }
        }
        
        // Compare sources count
        Assert.Equal(guiState.Sources.Count(), loadedState.Sources.Count());
        
        // Compare filters count
        Assert.Equal(guiState.Filters.Count(), loadedState.Filters.Count());
    }
    
    [Fact]
    public void TestMultipleSaveLoadSameObject()
    {
        // Test the issue with duplicating sources and filters when saving same object multiple times
        using var storage = new SignalStorage(":memory:");
        
        // Create some signal generators
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var genFactory = new ObjectFactory(sinusoidGen, []);
        
        // Create a filter
        var lowPassFilter = new LowPassFilter(0.8f);
        var filterFactory = new ObjectFactory(lowPassFilter, []);
        
        // Create a ComputeSignal
        var generators = new[] { ("A", (ISignalGenerator)sinusoidGen) };
        var ops = new ISignalOperation[] { lowPassFilter };
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
        
        // Create initial state
        var guiState = new GuiSignalState(
            objectName: "DuplicateTest",
            computedSignal: computeSignal.ComputedSignal,
            sources: new[] { genFactory },
            expression: "A",
            filters: new[] { (true, filterFactory) },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 50), ("renderPoints", 256)]
            )
        );
        
        // Save the state multiple times (this should overwrite, not duplicate)
        for (int i = 0; i < 3; i++)
        {
            var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
            storage.AddSessionState(sessionStateModel);
            
            // Load and verify
            var loadedState = GuiStateConverter.LoadFromDB(storage, "DuplicateTest");
            
            // The counts should remain the same, not increase with each save
            Assert.Single(loadedState.Sources);
            Assert.Single(loadedState.Filters);
        }
    }
}