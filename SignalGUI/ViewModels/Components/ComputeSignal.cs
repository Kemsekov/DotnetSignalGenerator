// TODO: separate somehow signal creation/plotting/UI gesture interaction
// logic into separate classes
using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using SignalCore;
using SignalCore.Computation;
using NumpyDotNet;
using Avalonia.Threading;
using System.Collections.Generic;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

/// <summary>
/// Class that implements non-blocking signal's computation logic
/// </summary>
public class ComputeSignal : ICloneable
{
    TrackedOperation<ndarray> combineSources;
    TrackedOperation<ndarray> createdSignal;
    TrackedOperation<(float stat, string name)[]> signalStatistics;

    // Properties to hold 1d signal data
    public float[]? X{get;protected set;}
    public float[]? Y{get;protected set;}
    public float[]? YImag{get;protected set;}
    // Properties to hold 2D data (like Wavelet(FWT) transform)
    public ndarray? ImageData{get;protected set;}
    // Signal statistics like mean, std, skew, amplitude, etc
    public SignalStatisticViewModel[]? Stats{get;protected set;}
    public ComputeSignal(
        int computePoints,
        IEnumerable<(string letter, ISignalGenerator instance)> generators,
        string expression,
        IEnumerable<ISignalOperation> ops,
        IEnumerable<ISignalStatistic> statistics)
    {
        ComputePoints = computePoints;
        Generators = generators;
        Expression = expression;
        Ops = ops;
        Statistics = statistics;
        
        var expr = new StringExpression(expression);
        // Method to create function that sample signal 
        // from given generator and assign a name to it
        Func<(string name, ndarray signal)> SignalFactory(ISignalGenerator g, string signalLetter)
            => () => (
                signalLetter,
                g.Sample(computePoints)
            );

        //this one creates signal sources
        var generationOperation = LazyTrackedOperation.Factory(
            generators.Select(v => SignalFactory(v.instance, v.letter)).ToArray()
        );

        // this one combines multiple sources into single signal
        combineSources =
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
        createdSignal = combineSources.Composition(
            [s=>s.at(1), // first select Y dimension
            // then apply filters,transforms, normalizations,etc
            ..
            ops.Select(v=>(Func<ndarray,ndarray>)v.Compute)]
        );

        // this one computes all signal statistics
        (float stat,string name) ComputeStatistic(ndarray signal, ISignalStatistic stat)
        {
            var value = np.round(stat.Compute(signal),3).AsFloatArray()[0];
            return (value,stat.Name);
        }

        //Compute all signal stats at parallel
        signalStatistics = createdSignal.Transform(
            statistics.Select(
                stat=>(Func<ndarray,(float stat,string name)>)(
                    signal=>ComputeStatistic(signal,stat)
                )
            ).ToArray()
        );

        signalStatistics.OnExecutionDone += statsRaw =>
        {
            Stats = statsRaw.Select(item => new SignalStatisticViewModel(item.name, item.stat)).ToArray();
            // Need to dispatch to UI thread since this event is called from background thread


            var res = createdSignal.Result;
            var genOut = combineSources?.Result;
            System.Console.WriteLine("====================");
            System.Console.WriteLine($"Time {genOut?.shape}");
            System.Console.WriteLine($"Signal {res.shape}");
            System.Console.WriteLine($"Signal dtype {res.Dtype}");
            // if we got single signal output of shape [1,N] or [N]
            if (res.shape[0] == 1 && res.shape.iDims.Length == 2 || res.shape.iDims.Length == 1)
            {
                // if we have complex outputs like FFT, keep separate real
                // and imag part of signal
                if (res.Dtype == np.Complex)
                    YImag = res.Imag?.AsFloatArray();
                else
                    YImag = null;

                // here X is signal time, Y is signal value

                // Store the X and Y values for plotting
                X = genOut?.at(0)?.AsFloatArray();
                Y = res.Real?.AsFloatArray();
            }

            // if we have 2d array, then render it as image
            if(res.shape[0]>1 && res.shape.iDims.Length == 2)
            {
                ImageData = res;
            }
            OnExecutionDone.Invoke();
        };
        signalStatistics.OnExecutedStep+=i=>OnExecutedStep(i);
        signalStatistics.CancelState.OnCancel+= ()=>OnCancel();
        signalStatistics.OnException+= e=> OnException(e);
    }
    public float PercentCompleted => signalStatistics.PercentCompleted;

    public int ComputePoints { get; }
    public IEnumerable<(string letter, ISignalGenerator instance)> Generators { get; }
    public string Expression { get; }
    public IEnumerable<ISignalOperation> Ops { get; }
    public IEnumerable<ISignalStatistic> Statistics { get; }

    public Action OnCancel = ()=>{};
    public event Action<Exception> OnException = e=>{};
    public event Action<int> OnExecutedStep = i=>{};
    public event Action OnExecutionDone = ()=>{};
    //Run the latest chain element
    public void Run() => signalStatistics.Run();
    // Cancel computation
    public void Cancel()=>signalStatistics.Cancel();

    public ComputeSignal Clone()
    {
        return new ComputeSignal(
            ComputePoints,
            Generators,
            Expression,
            Ops,
            Statistics
        )
        {
            ImageData=ImageData,
            Stats=Stats,
            X=X,
            Y=Y,
            YImag=YImag            
        };
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}

public partial class CompositeComponentViewModel
{
    [RelayCommand]
    private void ComputeSignal()
    {
        if (Sources.Count == 0) return;
        if (Expression == "") //if empty
            Expression = "A"; // just identity of first signal

        RenderedImage=null;
        CompletedPercent = 0;

        IEnumerable<(string letter, ISignalGenerator instance)>? generators;
        IEnumerable<ISignalOperation>? ops;
        SignalParameters? s;
        var expr=new StringExpression(Expression);
        var parse = ParseComputeArguments(out s,out generators, out ops);
        if(parse is Exception e)
        {
            //show exception
            ErrorHandlingUtils.ShowErrorWindow(e);
            return;
        }

        // to shut up warnings
        if(generators is null || ops is null)
            return;

        var computeSignal = new ComputeSignal(
            s?.ComputePoints ?? 1024,
            generators,
            Expression,
            ops,
            AvailableSignalStatistics
        );

        // Subscribe to the OnExecutedStep event to update completion percentage
        computeSignal.OnExecutedStep += (_) =>
        {
            var completed = computeSignal.PercentCompleted;
            // Update the CompletedPercent property by multiplying PercentCompleted by 100 and rounding to int
            var percent = (int)Math.Round(completed * 100);
            CompletedPercent = percent;
        };

        computeSignal.OnCancel+=()=>{
            CompletedPercent = -1;
        };

        // if something broke when computing show error
        computeSignal.OnException += e =>
        {
            Dispatcher.UIThread.Post(()=>ErrorHandlingUtils.ShowErrorWindow(e));
        };

        computeSignal.OnExecutionDone += () =>
        {
            // Update signal statistics
            Dispatcher.UIThread.Post(() =>
            {
                SignalStatistics = computeSignal.Stats;
            });
            // Automatically plot as a line chart after computation is done
            // Need to dispatch to UI thread since this event is called from background thread
            Dispatcher.UIThread.Post(PlotLine);
            Dispatcher.UIThread.Post(Plot2DImage);
        };

        computeSignal.Run();
        _computeSignal=computeSignal;
    }

    [RelayCommand]
    private void Cancel()
    {
        _computeSignal?.Cancel();
    }

    Exception? ParseComputeArguments(
        out SignalParameters? s,
        out IEnumerable<(string letter, ISignalGenerator instance)>? generators,
        out IEnumerable<ISignalOperation>? ops)
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
            s = SignalParameters;
            return null;
        }
        catch(Exception e)
        {
            generators = null;
            ops = null;
            s=null;
            return e;
        }

        
    }
    
}