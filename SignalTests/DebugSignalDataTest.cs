using System;
using System.Linq;
using SignalCore;
using SignalCore.GuiState;
using SignalCore.Storage;
using SignalCore.Parameters;
using Xunit;

namespace SignalTests;

public class DebugSignalDataTest
{
    [Fact]
    public void TestSignalDataStorage()
    {
        // Create an in-memory database for testing
        using var storage = new SignalStorage(":memory:");
        
        // Create a simple ComputeSignal to generate actual signal data
        var generators = new[]
        {
            ("A", (ISignalGenerator)new SinusoidGenerator(amplitude: 1, frequency: 1))
        };
        
        var ops = new ISignalOperation[] {};
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
        
        Console.WriteLine($"Original signal X length: {computeSignal.ComputedSignal.X?.Length}");
        Console.WriteLine($"Original signal Y length: {computeSignal.ComputedSignal.Y?.Length}");
        Console.WriteLine($"Original signal X[0]: {computeSignal.ComputedSignal.X?[0]}");
        Console.WriteLine($"Original signal Y[0]: {computeSignal.ComputedSignal.Y?[0]}");
        
        // Create a GuiSignalState with the computed signal
        var guiState = new GuiSignalState(
            objectName: "DebugTest",
            computedSignal: computeSignal.ComputedSignal,
            sources: new[] { new ObjectFactory(generators[0].Item2, []) },
            expression: "A",
            filters: new (bool, ObjectFactory)[0],
            signalParams: new ObjectFactory(
                typeof(SignalParameters),
                args: [("computePoints", 50), ("renderPoints", 256)]
            )
        );
        
        // Convert to session state model
        var sessionStateModel = GuiStateConverter.ToSessionStateModel(guiState);
        
        Console.WriteLine($"Session signal X length after ToSessionStateModel: {sessionStateModel.Session.SignalX.GetNdarray().numel()}");
        Console.WriteLine($"Session signal Y length after ToSessionStateModel: {sessionStateModel.Session.SignalY.GetNdarray().numel()}");
        
        // Save to database
        storage.AddSessionState(sessionStateModel);
        
        // Load from database
        var loadedState = GuiStateConverter.LoadFromDB(storage, "DebugTest");
        
        Console.WriteLine($"Loaded signal X length: {loadedState.ComputedSignal.X?.Length}");
        Console.WriteLine($"Loaded signal Y length: {loadedState.ComputedSignal.Y?.Length}");
        Console.WriteLine($"Loaded signal X[0]: {loadedState.ComputedSignal.X?[0]}");
        Console.WriteLine($"Loaded signal Y[0]: {loadedState.ComputedSignal.Y?[0]}");
        
        // Verify that the loaded state has the same signal data
        Assert.NotNull(loadedState.ComputedSignal.X);
        Assert.NotNull(loadedState.ComputedSignal.Y);
        Assert.Equal(50, loadedState.ComputedSignal.X.Length);
        Assert.Equal(50, loadedState.ComputedSignal.Y.Length);
    }
}