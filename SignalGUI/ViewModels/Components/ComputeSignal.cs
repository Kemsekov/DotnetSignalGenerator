using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using SignalCore;
using SignalCore.Computation;
using NumpyDotNet;
using Avalonia.Threading;
using System.Collections.Generic;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel
{
    [RelayCommand]
    private void ComputeSignal()
    {
        if (Sources.Count == 0) return;
        if (Expression == "")
            Expression = "A"; // just identity of first signal

        CompletedPercent = 0;

        SignalParameters? args;
        IEnumerable<(string letter, ISignalGenerator instance)>? generators;
        IEnumerable<ISignalOperation>? ops;
        StringExpression? expr;
        var parse = ParseComputeArguments(out args, out generators, out ops, out expr);
        if(parse is Exception e)
        {
            System.Console.WriteLine(e.GetMostInnerException());
            //handle exception
            return;
        }
        // to shut up warnings
        if(args is null || generators is null || ops is null || expr is null)
            return;


        // yeah, that's ugly
        Func<(string name, ndarray signal)> SignalFactory(ISignalGenerator g, string signalLetter)
            => () => (
                signalLetter,
                g.Sample(args.Points)
            );


        //this one creates signal sources
        var generationOperation = LazyTrackedOperation.Factory(
            generators.Select(v => SignalFactory(v.instance, v.letter)).ToArray()
        );

        // this one combines multiple sources into single signal
        var combineSources =
            generationOperation
            .Transform(v =>
                // drop X values before applying expression
                v.Select(pair => (pair.name, pair.signal.at(1)))
                .ToArray()
            )
            .Transform(expr.Call)
            .Transform(s =>
            {
                //Add again X value after expression is applied
                var X = generationOperation.Result[0].signal.at(0);
                return np.concatenate([X, s]);
            });

        //this one applies filters/transformations/normalizations/etc
        var createdSignal = combineSources.Composition(
            [s=>s.at(1), // first select Y dimension
            // then apply filters,transforms,etc
            ..
            ops.Select(v=>(Func<ndarray,ndarray>)v.Compute)]
        );

        // Subscribe to the OnExecutedStep event to update completion percentage
        createdSignal.OnExecutedStep += (_) =>
        {
            // Update the CompletedPercent property by multiplying PercentCompleted by 100 and rounding to int
            var percent = (int)Math.Round(createdSignal.PercentCompleted * 100);
            CompletedPercent = percent;
        };

        // this one starts this operations chain computation
        createdSignal.Run();

        // this one tells whether the signal is still computing
        //createdSignal.IsRunning

        _createdSignal = createdSignal;
        // This one tells how long it took to create signal so far
        //createdSignal.ElapsedMilliseconds

        // if something broke when computing
        createdSignal.OnException += e =>
        {
            var inner = e.GetMostInnerException();
            System.Console.WriteLine(inner.Message);
        };

        // This event called once computation is completed
        createdSignal.OnExecutionDone += res =>
        {
            var genOut = combineSources?.Result;
            System.Console.WriteLine("====================");
            System.Console.WriteLine(res.shape);
            System.Console.WriteLine(genOut?.shape);
            // if we got single signal output of shape [1,N]
            if (res.shape[0] == 1 && res.shape.iDims.Length == 2 || res.shape.iDims.Length == 1)
            {
                if (res.Dtype == np.Complex)
                    _yImagValues = res.Imag?.AsFloatArray();
                else
                    _yImagValues = null;

                // here X is signal time, Y is signal value

                // Store the X and Y values for plotting
                _xValues = genOut?.at(0)?.AsFloatArray();
                _yValues = res.Real?.AsFloatArray();
                // Automatically plot as a line chart after computation is done
                // Need to dispatch to UI thread since this event is called from background thread
                Dispatcher.UIThread.Post(() =>
                {
                    PlotLine();
                });
            }
        };
    }

    Exception? ParseComputeArguments(
        out SignalParameters? args, 
        out IEnumerable<(string letter, ISignalGenerator instance)>? generators, 
        out IEnumerable<ISignalOperation>? ops, 
        out StringExpression? expr)
    {
        try{
            var sources = Sources
                .Where(v => v.Factory is not null)
                .Select(v => new { 
                    letter = v.Letter, 
                    instance = v.Factory?.GetInstance() })
                .Where(s => s.instance is not null)
                .ToList();

            var signalEdit = Filters
                .Where(v => v.Factory is not null && v.Enabled)
                .Select(v => v.Factory?.GetInstance())
                .Where(s => s != null)
                .ToList();

            args = SignalParams?.GetInstance() as SignalParameters ?? 
                throw new ArgumentException("Failed to cast SignalParameters");
            generators = sources
                .Where(s => s.instance is ISignalGenerator)
                .Select(s => (
                    s.letter,
                    s.instance as ISignalGenerator ?? throw new Exception() //impossible exception
                ))
                .ToList();
            ops = signalEdit
                .Where(s => s is ISignalOperation)
                .Cast<ISignalOperation>()
                .ToList();
            expr = new StringExpression(Expression);
            return null;
        }
        catch(Exception e)
        {
            args = null;
            generators = null;
            ops = null;
            expr = null;
            return e;
        }
    }
    
}