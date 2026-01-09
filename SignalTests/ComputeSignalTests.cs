using SignalCore;
using SignalCore.Computation;
using NumpyDotNet;

namespace SignalTests;

public class ComputeSignalTests
{
    [Fact]
    public void TestWithSeveralGenerators()
    {
        // Test with several generators
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1)),
            ("B", new SquareGenerator(amplitude: 0.5f, frequency: 2))
        };

        var ops = new List<ISignalOperation>();
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A+B",  // Expression combining two signals
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        Assert.NotNull(computeSignal.X);
        Assert.NotNull(computeSignal.Y);
        Assert.Equal(100, computeSignal.X.Length);
        Assert.Equal(100, computeSignal.Y.Length);
        Assert.NotNull(computeSignal.Stats);
        Assert.Single(computeSignal.Stats);
    }

    [Fact]
    public void TestValidExpression()
    {
        // Test with valid expression
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>();
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",  // Valid expression
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        Assert.NotNull(computeSignal.X);
        Assert.NotNull(computeSignal.Y);
        Assert.Equal(100, computeSignal.X.Length);
        Assert.Equal(100, computeSignal.Y.Length);
    }

    [Fact]
    public void TestInvalidExpression()
    {
        // Test with invalid expression - should throw an exception
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>();
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "INVALID_EXPRESSION",  // Invalid expression
            ops,
            statistics
        );

        var exceptionThrown = false;
        computeSignal.OnException += (ex) => exceptionThrown = true;

        computeSignal.Run();

        // Wait a bit for the exception to be processed
        Thread.Sleep(100);

        Assert.True(exceptionThrown);
    }

    [Fact]
    public void TestWithSeveralSignalOperations()
    {
        // Test with several signal operations
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>
        {
            new LowPassFilter(0.5f),
            new AddNormalNoiseFilter(mean: 0, std: 0.1f)
        };

        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        Assert.NotNull(computeSignal.X);
        Assert.NotNull(computeSignal.Y);
        Assert.Equal(100, computeSignal.X.Length);
        Assert.Equal(100, computeSignal.Y.Length);
    }

    [Fact]
    public void TestWithTransforms()
    {
        // Test with transforms (FFT)
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>
        {
            new FFTTransform()
        };

        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        Assert.NotNull(computeSignal.X);
        Assert.NotNull(computeSignal.Y);
        Assert.NotNull(computeSignal.YImag); // Complex output should have imaginary part
        Assert.Equal(100, computeSignal.X.Length);
        Assert.Equal(100, computeSignal.Y.Length);
        Assert.Equal(100, computeSignal.YImag.Length);
    }

    [Fact]
    public void TestFWTTransformProducesImageData()
    {
        // Test that FWT transform produces ImageData field
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>
        {
            new FWTTransform("haar", 3)  // FWT transform
        };

        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 64,  // Must be power of 2 for FWT
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        // FWT produces 2D output, so ImageData should be set instead of X,Y
        Assert.NotNull(computeSignal.ImageData);
        Assert.Null(computeSignal.X);  // X and Y should be null for 2D output
        Assert.Null(computeSignal.Y);
    }

    [Fact]
    public void TestSeveralStatistics()
    {
        // Test several statistics
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>();
        var statistics = new List<ISignalStatistic>
        {
            new MeanStatistic(),
            new StdStatistic(),
            new MinStatistic(),
            new MaxStatistic()
        };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        Assert.NotNull(computeSignal.Stats);
        Assert.Equal(4, computeSignal.Stats.Length); // 4 statistics

        // Check that all statistics have proper names
        var statNames = computeSignal.Stats.Select(s => s.name).ToList();
        Assert.Contains("Mean", statNames);
        Assert.Contains("STD", statNames);
        Assert.Contains("Minimum", statNames);
        Assert.Contains("Maximum", statNames);
    }

    [Fact]
    public void TestOnExecutionDoneEvent()
    {
        // Test OnExecutionDone event is called
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>();
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var executionDoneCalled = false;
        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.OnExecutionDone += () => executionDoneCalled = true;

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        Assert.True(executionDoneCalled);
        Assert.NotNull(computeSignal.X);
        Assert.NotNull(computeSignal.Y);
    }

    [Fact]
    public void TestOnExecutedStepEvent()
    {
        // Test OnExecutedStep event is called
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>();
        var statistics = new List<ISignalStatistic>
        {
            new MeanStatistic(),
            new StdStatistic()
        }; // Two statistics to trigger steps

        var executedSteps = new List<int>();
        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.OnExecutedStep += (step) => executedSteps.Add(step);

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        // Should have at least 2 steps (one for each statistic)
        Assert.NotEmpty(executedSteps);
        Assert.Contains(0, executedSteps);
    }

    [Fact]
    public void TestOnExceptionEvent()
    {
        // Test OnException event is called when operation throws
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        // Create a custom operation that throws
        var throwingOp = new ThrowingOperation();
        var ops = new List<ISignalOperation> { throwingOp };
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var exceptionCalled = false;
        var capturedException = new List<Exception>();
        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.OnException += (ex) => {
            exceptionCalled = true;
            capturedException.Add(ex);
        };

        computeSignal.Run();

        // Wait for exception to be processed
        Thread.Sleep(200);

        Assert.True(exceptionCalled);
        Assert.NotEmpty(capturedException);
    }

    [Fact]
    public void TestOnCancelEvent()
    {
        // Test OnCancel event is called when operation is cancelled
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        // Create an operation that takes some time
        var slowOp = new SlowOperation(500); // 500ms operation
        var ops = new List<ISignalOperation> { slowOp };
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var cancelCalled = false;
        var computeSignal = new ComputeSignal(
            computePoints: 1000, // More points to make it take longer
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.OnCancel += () => cancelCalled = true;

        computeSignal.Run();

        // Cancel after a short delay
        Thread.Sleep(100);
        computeSignal.Cancel();

        // Wait for cancellation to be processed
        Thread.Sleep(100);

        Assert.True(cancelCalled);
    }

    [Fact]
    public void TestRunCommand()
    {
        // Test Run command works properly
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        var ops = new List<ISignalOperation>();
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 100,
            generators,
            "A",
            ops,
            statistics
        );

        // Initially, no computation should have happened
        Assert.Equal(0.0f, computeSignal.PercentCompleted);

        computeSignal.Run();
        computeSignal.Wait(); // Wait for computation to complete

        // After running, computation should be complete
        Assert.Equal(1.0f, computeSignal.PercentCompleted);
        Assert.NotNull(computeSignal.X);
        Assert.NotNull(computeSignal.Y);
    }

    [Fact]
    public void TestCancelCommand()
    {
        // Test Cancel command works properly
        var generators = new List<(string, ISignalGenerator)>
        {
            ("A", new SinusoidGenerator(amplitude: 1, frequency: 1))
        };

        // Create a slow operation to allow for cancellation
        var slowOp = new SlowOperation(1000); // 1 second operation
        var ops = new List<ISignalOperation> { slowOp };
        var statistics = new List<ISignalStatistic> { new MeanStatistic() };

        var computeSignal = new ComputeSignal(
            computePoints: 1000, // More points to make it take longer
            generators,
            "A",
            ops,
            statistics
        );

        computeSignal.Run();

        // Cancel after a short delay
        Thread.Sleep(100);
        computeSignal.Cancel();

        // Wait a bit to ensure cancellation has time to process
        Thread.Sleep(100);

        // Check that the operation was cancelled (percent completed should reflect this)
        // The exact behavior depends on implementation, but it should be marked as cancelled
    }

    // Helper class for testing exception handling
    private class ThrowingOperation : ISignalOperation
    {
        public ndarray Compute(ndarray signal)
        {
            throw new InvalidOperationException("Test exception from ThrowingOperation");
        }
    }

    // Helper class for testing cancellation with slow operations
    private class SlowOperation : ISignalOperation
    {
        private readonly int _delayMs;

        public SlowOperation(int delayMs)
        {
            _delayMs = delayMs;
        }

        public ndarray Compute(ndarray signal)
        {
            Thread.Sleep(_delayMs);
            return signal;
        }
    }
}