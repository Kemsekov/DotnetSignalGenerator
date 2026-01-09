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


public partial class CompositeComponentViewModel
{
    [RelayCommand]
    void ComputeSignal()
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
                SignalStatistics = computeSignal.Stats?.Select(v=> new SignalStatisticViewModel(v.name,v.stat)).ToArray();
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
    void Cancel()
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