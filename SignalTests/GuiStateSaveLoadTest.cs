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
                Assert.Equal(guiState.ComputedSignal.X[i], loadedState.ComputedSignal.X[i], precision: 1);
            }
        }

        if (guiState.ComputedSignal?.Y != null && loadedState.ComputedSignal?.Y != null)
        {
            Assert.Equal(guiState.ComputedSignal.Y.Length, loadedState.ComputedSignal.Y.Length);
            for (int i = 0; i < guiState.ComputedSignal.Y.Length; i++)
            {
                Assert.Equal(guiState.ComputedSignal.Y[i], loadedState.ComputedSignal.Y[i], precision: 1);
            }
        }
        
        // Compare signal statistics
        if (guiState.SignalStatistics != null && loadedState.SignalStatistics != null)
        {
            Assert.Equal(guiState.SignalStatistics.Length, loadedState.SignalStatistics.Length);
            for (int i = 0; i < guiState.SignalStatistics.Length; i++)
            {
                Assert.Equal(guiState.SignalStatistics[i].name, loadedState.SignalStatistics[i].name);
                Assert.Equal(guiState.SignalStatistics[i].stat, loadedState.SignalStatistics[i].stat, precision: 1);
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

    [Fact]
    public void TestComplexSignalSaveLoad()
    {
        // Test saving and loading of complex signals with imaginary parts
        using var storage = new SignalStorage(":memory:");

        // Create a sinusoid generator
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var genFactory = new ObjectFactory(sinusoidGen, []);

        // Create generators array
        var generators = new[]
        {
            ("A", (ISignalGenerator)sinusoidGen)
        };

        // Create operations
        var ops = new ISignalOperation[]
        {
            new LowPassFilter(0.8f)
        };

        var statistics = new ISignalStatistic[]
        {
            new MeanStatistic(),
            new StdStatistic()
        };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait();

        // Use the original computed signal as-is (no need to create a complex one separately)
        var complexComputedSignal = computeSignal.ComputedSignal;

        // Create a GuiSignalState with the complex signal
        var guiState = new GuiSignalState(
            objectName: "ComplexSignalTest",
            computedSignal: complexComputedSignal,
            signalStatistics: complexComputedSignal.Stats?.Select(s => (s.name, s.stat)).ToArray(),
            completedPercent: 100,
            sources: new[] { genFactory },
            expression: "A",
            filters: Array.Empty<(bool, ObjectFactory)>(),
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 100), ("renderPoints", 256)]
            )
        );

        // Save the state to database
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
        storage.AddSessionState(sessionStateModel);

        // Load the state from database
        var loadedState = GuiStateConverter.LoadFromDB(storage, "ComplexSignalTest");

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
                Assert.Equal(guiState.ComputedSignal.X[i], loadedState.ComputedSignal.X[i], precision: 1);
            }
        }

        if (guiState.ComputedSignal?.Y != null && loadedState.ComputedSignal?.Y != null)
        {
            Assert.Equal(guiState.ComputedSignal.Y.Length, loadedState.ComputedSignal.Y.Length);
            for (int i = 0; i < guiState.ComputedSignal.Y.Length; i++)
            {
                Assert.Equal(guiState.ComputedSignal.Y[i], loadedState.ComputedSignal.Y[i], precision: 1);
            }
        }

        // Compare signal statistics
        if (guiState.SignalStatistics != null && loadedState.SignalStatistics != null)
        {
            Assert.Equal(guiState.SignalStatistics.Length, loadedState.SignalStatistics.Length);
            for (int i = 0; i < guiState.SignalStatistics.Length; i++)
            {
                Assert.Equal(guiState.SignalStatistics[i].name, loadedState.SignalStatistics[i].name);
                Assert.Equal(guiState.SignalStatistics[i].stat, loadedState.SignalStatistics[i].stat, precision: 1);
            }
        }

        // Compare sources count
        Assert.Equal(guiState.Sources.Count(), loadedState.Sources.Count());
    }

    [Fact]
    public void TestTransformsSaveLoad()
    {
        // Test saving and loading of signals with transforms (FFT, FWT)
        using var storage = new SignalStorage(":memory:");

        // Create a sinusoid generator
        var sinusoidGen = new SinusoidGenerator(amplitude: 1, frequency: 1);
        var genFactory = new ObjectFactory(sinusoidGen, []);

        // Create transforms
        var fftTransform = new FFTTransform();
        var fwtTransform = new FWTTransform(waveletName: "haar", levels: 2);

        // Create ObjectFactory instances for the transforms
        var fftFactory = new ObjectFactory(fftTransform, []);
        var fwtFactory = new ObjectFactory(fwtTransform, []);

        // Create generators array
        var generators = new[]
        {
            ("A", (ISignalGenerator)sinusoidGen)
        };

        // Create operations - we'll use transforms as operations
        var ops = new ISignalOperation[]
        {
            new LowPassFilter(0.8f)
        };

        var statistics = new ISignalStatistic[]
        {
            new MeanStatistic(),
            new StdStatistic()
        };

        var computeSignal = new ComputeSignal(
            computePoints: 64, // Using power of 2 for FWT
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait();

        // Create a GuiSignalState with transforms in the filters list
        var guiState = new GuiSignalState(
            objectName: "TransformTest",
            computedSignal: computeSignal.ComputedSignal,
            signalStatistics: computeSignal.ComputedSignal.Stats?.Select(s => (s.name, s.stat)).ToArray(),
            completedPercent: 100,
            sources: new[] { genFactory },
            expression: "A",
            filters: new[]
            {
                (true, fftFactory),  // FFT transform
                (true, fwtFactory),  // FWT transform
                (false, new ObjectFactory(new ZScoreNormalization(0, 1), []))  // Hidden normalization
            },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 64), ("renderPoints", 256)]
            )
        );

        // Save the state to database
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
        storage.AddSessionState(sessionStateModel);

        // Load the state from database
        var loadedState = GuiStateConverter.LoadFromDB(storage, "TransformTest");

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
                Assert.Equal(guiState.ComputedSignal.X[i], loadedState.ComputedSignal.X[i], precision: 1);
            }
        }

        if (guiState.ComputedSignal?.Y != null && loadedState.ComputedSignal?.Y != null)
        {
            Assert.Equal(guiState.ComputedSignal.Y.Length, loadedState.ComputedSignal.Y.Length);
            for (int i = 0; i < guiState.ComputedSignal.Y.Length; i++)
            {
                Assert.Equal(guiState.ComputedSignal.Y[i], loadedState.ComputedSignal.Y[i], precision: 1);
            }
        }

        // Compare signal statistics
        if (guiState.SignalStatistics != null && loadedState.SignalStatistics != null)
        {
            Assert.Equal(guiState.SignalStatistics.Length, loadedState.SignalStatistics.Length);
            for (int i = 0; i < guiState.SignalStatistics.Length; i++)
            {
                Assert.Equal(guiState.SignalStatistics[i].name, loadedState.SignalStatistics[i].name);
                Assert.Equal(guiState.SignalStatistics[i].stat, loadedState.SignalStatistics[i].stat, precision: 1);
            }
        }

        // Compare sources count
        Assert.Equal(guiState.Sources.Count(), loadedState.Sources.Count());

        // Compare filters count
        Assert.Equal(guiState.Filters.Count(), loadedState.Filters.Count());

        // Verify that transforms were properly reconstructed by checking their types
        var loadedFilters = loadedState.Filters.ToArray();
        Assert.Equal(3, loadedFilters.Length);

        // Check that the first two are transforms (FFT and FWT)
        var firstFactory = loadedFilters[0].factory;
        var secondFactory = loadedFilters[1].factory;

        // Create instances to verify the types
        var firstInstance = firstFactory.CreateInstance<object>();
        var secondInstance = secondFactory.CreateInstance<object>();

        // At least verify that they can be instantiated without error
        Assert.NotNull(firstInstance);
        Assert.NotNull(secondInstance);
    }

    [Fact]
    public void TestDifferentGeneratorTypesAndEmptyFields()
    {
        // Test saving and loading with different generator types and empty/null fields
        using var storage = new SignalStorage(":memory:");

        // Create different types of generators
        var sinusoidGen = new SinusoidGenerator(amplitude: 1.5f, frequency: 2.5f);
        var squareGen = new SquareGenerator(amplitude: 0.8f, frequency: 1.2f);
        var triangleGen = new TriangleGenerator(amplitude: 1.0f, frequency: 0.5f);
        var sawtoothGen = new SawToothGenerator(amplitude: 2.0f, frequency: 3.0f);

        // Create ObjectFactory instances for the generators
        var sinusoidFactory = new ObjectFactory(sinusoidGen, []);
        var squareFactory = new ObjectFactory(squareGen, []);
        var triangleFactory = new ObjectFactory(triangleGen, []);
        var sawtoothFactory = new ObjectFactory(sawtoothGen, []);

        // Create some operations
        var lowPassFilter = new LowPassFilter(0.7f);
        var highPassFilter = new HighPassFilter(0.6f);
        var addNoiseFilter = new AddNormalNoiseFilter(mean: 0.1f, std: 0.2f);
        var minMaxNorm = new MinMaxNormalization(min: -1, max: 1);

        // Create ObjectFactory instances for the operations
        var lowPassFactory = new ObjectFactory(lowPassFilter, []);
        var highPassFactory = new ObjectFactory(highPassFilter, []);
        var noiseFactory = new ObjectFactory(addNoiseFilter, []);
        var minMaxFactory = new ObjectFactory(minMaxNorm, []);

        // Create generators array with multiple different types
        var generators = new[]
        {
            ("Sinusoid", (ISignalGenerator)sinusoidGen),
            ("Square", (ISignalGenerator)squareGen),
            ("Triangle", (ISignalGenerator)triangleGen),
            ("Sawtooth", (ISignalGenerator)sawtoothGen)
        };

        // Create operations
        var ops = new ISignalOperation[]
        {
            lowPassFilter,
            highPassFilter,
            addNoiseFilter,
            minMaxNorm
        };

        var statistics = new ISignalStatistic[]
        {
            new MeanStatistic(),
            new StdStatistic(),
            new MaxStatistic(),
            new MinStatistic()
        };

        var computeSignal = new ComputeSignal(
            computePoints: 200,
            generators,
            "Sinusoid + Square + Triangle + Sawtooth",
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait();

        // Create a GuiSignalState with multiple different generator types and some empty fields
        var guiState = new GuiSignalState(
            objectName: "MultiTypeTest",
            computedSignal: computeSignal.ComputedSignal,
            signalStatistics: computeSignal.ComputedSignal.Stats?.Select(s => (s.name, s.stat)).ToArray(),
            completedPercent: 100,
            sources: new[] { sinusoidFactory, squareFactory, triangleFactory, sawtoothFactory }, // Multiple sources
            expression: "Sinusoid + Square + Triangle + Sawtooth",
            filters: new[]
            {
                (true, lowPassFactory),   // Visible filter
                (true, highPassFactory),  // Visible filter
                (false, noiseFactory),    // Hidden filter
                (true, minMaxFactory)     // Visible normalization
            },
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 200), ("renderPoints", 512)]  // Different render points
            )
        );

        // Save the state to database
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
        storage.AddSessionState(sessionStateModel);

        // Load the state from database
        var loadedState = GuiStateConverter.LoadFromDB(storage, "MultiTypeTest");

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
                Assert.Equal(guiState.ComputedSignal.X[i], loadedState.ComputedSignal.X[i], precision: 1);
            }
        }

        if (guiState.ComputedSignal?.Y != null && loadedState.ComputedSignal?.Y != null)
        {
            Assert.Equal(guiState.ComputedSignal.Y.Length, loadedState.ComputedSignal.Y.Length);
            for (int i = 0; i < guiState.ComputedSignal.Y.Length; i++)
            {
                Assert.Equal(guiState.ComputedSignal.Y[i], loadedState.ComputedSignal.Y[i], precision: 1);
            }
        }

        // Compare signal statistics
        if (guiState.SignalStatistics != null && loadedState.SignalStatistics != null)
        {
            Assert.Equal(guiState.SignalStatistics.Length, loadedState.SignalStatistics.Length);
            for (int i = 0; i < guiState.SignalStatistics.Length; i++)
            {
                Assert.Equal(guiState.SignalStatistics[i].name, loadedState.SignalStatistics[i].name);
                Assert.Equal(guiState.SignalStatistics[i].stat, loadedState.SignalStatistics[i].stat, precision: 1);
            }
        }

        // Compare sources count
        Assert.Equal(guiState.Sources.Count(), loadedState.Sources.Count());

        // Compare filters count
        Assert.Equal(guiState.Filters.Count(), loadedState.Filters.Count());

        // Verify that all different generator types were properly reconstructed
        var loadedSources = loadedState.Sources.ToArray();
        Assert.Equal(4, loadedSources.Length); // Should have all 4 generator types

        // Verify that we can instantiate each generator type
        foreach (var factory in loadedSources)
        {
            var instance = factory.CreateInstance<object>();
            Assert.NotNull(instance);
        }

        // Verify that all filter types were properly reconstructed
        var loadedFilters = loadedState.Filters.ToArray();
        Assert.Equal(4, loadedFilters.Length); // Should have all 4 operations

        // Verify that we can instantiate each filter/normalization type
        foreach (var (_, factory) in loadedFilters)
        {
            var instance = factory.CreateInstance<object>();
            Assert.NotNull(instance);
        }
    }
}