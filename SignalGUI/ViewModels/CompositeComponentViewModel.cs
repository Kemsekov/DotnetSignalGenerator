using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignalCore;
using SignalGUI.Utils;
using SignalCore.Computation;
using NumpyDotNet;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Avalonia.Threading;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel : ViewModelBase
{
    TrackedOperation<ndarray>? _createdSignal;

    [ObservableProperty]
    private string _objectName = "NewObject";

    [ObservableProperty]
    private int _completedPercent = 0;

    [ObservableProperty]
    private string _expression = "";
    private int _nextSourceLetterIndex = 0;
    public ObservableCollection<SourceItemViewModel> Sources { get; set; } = new();
    public ObservableCollection<FilterItemViewModel> Filters { get; set; } = new();
    
    [ObservableProperty]
    private SourceItemViewModel? _selectedSource;

    [ObservableProperty]
    private FilterItemViewModel? _selectedFilter;

    [ObservableProperty]
    private GuiObjectFactory? _signalParams = SignalParameters.CreateFactory();
    public List<GuiObjectFactory> AvailableSourceTypes { get; set; }
        = typeof(ISignalGenerator)
        .GetAllImplementations()
        .Select(type=>
            new{type, ctor=type.GetSupportedConstructor(ArgumentsTypesUtils.SupportedTypes)}
        )
        .Where(v=>v.ctor is not null)
        .Select(v=>new GuiObjectFactory(v.type,v.ctor ?? throw new Exception()))
        .ToList();

    public List<GuiObjectFactory> AvailableFilterTypes { get; set; } 
        = 
        typeof(IFilter).GetAllImplementations()
        .Concat(typeof(INormalization).GetAllImplementations())
        .Concat(typeof(ITransform).GetAllImplementations())
        .Select(type=>
            new{type, ctor=type.GetSupportedConstructor(ArgumentsTypesUtils.SupportedTypes)}
        )
        .Where(v=>v.ctor is not null)
        .Select(v=>new GuiObjectFactory(v.type,v.ctor ?? throw new Exception()))
        .ToList();

    public List<string> AvailableSourcesForExpression => Sources.Select(s => s.Letter).ToList();

    // Chart properties
    [ObservableProperty]
    private ObservableCollection<ISeries> _series = new();

    [ObservableProperty]
    private List<Axis> _xAxes = new() { new Axis { Name = "Time" } };

    [ObservableProperty]
    private List<Axis> _yAxes = new() { new Axis { Name = "Amplitude" } };

    private float[]? _xValues;
    private float[]? _yValues;
    private float[]? _yImagValues;

    [ObservableProperty]
    private ObservableCollection<ParameterViewModelWithCallback> _currentParameters = new();


    [RelayCommand]
    private void AddSources(GuiObjectFactory selectedSourceType)
    {
        
        if (selectedSourceType == null) return;

        var letter = GetNextLetter();
        var sourceItem = new SourceItemViewModel
        {
            Letter = letter,
            Factory = selectedSourceType // Store the factory instead of a string
        };

        Sources.Add(sourceItem);
        OnPropertyChanged(nameof(AvailableSourcesForExpression));
    }

    [RelayCommand]
    private void RemoveSource(SourceItemViewModel source)
    {
        Sources.Remove(source);
        ReassignSourceLetters();
        OnPropertyChanged(nameof(AvailableSourcesForExpression));
    }

    [RelayCommand]
    private void AddFilters(GuiObjectFactory selectedFilterType)
    {
        if (selectedFilterType == null) return;

        var filterItem = new FilterItemViewModel
        {
            Factory = selectedFilterType // Store the factory instead of a string
        };

        Filters.Add(filterItem);
    }

    [RelayCommand]
    private void RemoveFilter(FilterItemViewModel filter)
    {
        Filters.Remove(filter);
    }

    [RelayCommand]
    private void MoveFilterUp(FilterItemViewModel filter)
    {
        int index = Filters.IndexOf(filter);
        if (index > 0)
        {
            Filters.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveFilterDown(FilterItemViewModel filter)
    {
        int index = Filters.IndexOf(filter);
        if (index < Filters.Count - 1)
        {
            Filters.Move(index, index + 1);
        }
    }

    // Drag and drop methods for filters
    public void MoveFilterAt(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < Filters.Count &&
            newIndex >= 0 && newIndex < Filters.Count &&
            oldIndex != newIndex)
        {
            var item = Filters[oldIndex];
            Filters.RemoveAt(oldIndex);
            Filters.Insert(newIndex, item);
        }
    }

    [RelayCommand]
    public void SelectSource(SourceItemViewModel source)
    {
        SelectedSource = source;
        SelectedFilter = null; // Deselect filter if source is selected
        UpdateCurrentParameters();
    }

    [RelayCommand]
    public void SelectFilter(FilterItemViewModel filter)
    {
        SelectedFilter = filter;
        SelectedSource = null; // Deselect source if filter is selected
        UpdateCurrentParameters();
    }

    [RelayCommand]
    public void SelectSignalParams()
    {
        SelectedSource = null;
        SelectedFilter = null;
        UpdateCurrentParametersForSignalParams();
    }

    private void UpdateCurrentParameters()
    {
        CurrentParameters.Clear();

        if (SelectedSource?.Factory != null)
        {
            foreach (var param in SelectedSource.Factory.Arguments)
            {
                var value = SelectedSource.Factory.InstanceArguments[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value, value, (newValue) => {
                    SelectedSource.Factory.InstanceArguments[param.Key] = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }
        else if (SelectedFilter?.Factory != null)
        {
            foreach (var param in SelectedFilter.Factory.Arguments)
            {
                var value = SelectedFilter.Factory.InstanceArguments[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value, value, (newValue) => {
                    SelectedFilter.Factory.InstanceArguments[param.Key] = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }
    }

    private void UpdateCurrentParametersForSignalParams()
    {
        CurrentParameters.Clear();

        if (SignalParams != null)
        {
            foreach (var param in SignalParams.Arguments)
            {
                var value = SignalParams.InstanceArguments[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value, value, (newValue) => {
                    SignalParams.InstanceArguments[param.Key] = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }
    }

    private string GetNextLetter()
    {
        char letter = (char)('A' + _nextSourceLetterIndex);
        _nextSourceLetterIndex++;
        return letter.ToString();
    }

    private void ReassignSourceLetters()
    {
        _nextSourceLetterIndex = 0;
        foreach (var source in Sources)
        {
            source.Letter = GetNextLetter();
        }
    }

    [RelayCommand]
    private void ComputeSignal()
    {
        if(Sources.Count==0) return;
        if(Expression=="")
            Expression="A"; // just identity of first signal

        CompletedPercent=0;
        var sources =
            Sources.Where(v => v.Factory != null).Select(v => new{letter=v.Letter, instance=v.Factory?.GetInstance()}).Where(s => s.instance != null).ToList();

        var signalEdit =
            Filters.Where(v => v.Factory != null && v.Enabled).Select(v => v.Factory?.GetInstance()).Where(s => s != null).ToList();

        var args = SignalParams?.GetInstance() as SignalParameters ?? throw new ArgumentException("Failed to cast SignalParameters");

        var generators = sources
            .Where(s=>s.instance is ISignalGenerator)
            .Select(s=>new{
                s.letter,
                instance=s.instance as ISignalGenerator ?? throw new Exception() //impossible exception
            });
        var ops = signalEdit.Where(s=>s is ISignalOperation).Cast<ISignalOperation>();

        var expr = new StringExpression(Expression);


        // yeah, that's ugly
        Func<(string name, ndarray signal)> SignalFactory(ISignalGenerator g,string signalLetter)
            => () => (
                signalLetter,
                g.Sample(args.Points)
            );


        //this one creates signal sources
        var generationOperation = LazyTrackedOperation.Factory(
            generators.Select(v=>SignalFactory(v.instance,v.letter)).ToArray()
        );

        // this one combines multiple sources into single signal
        var combineSources = 
            generationOperation
            .Transform(v=>
                // drop X values before applying expression
                v.Select(pair=>(pair.name,pair.signal.at(1)))
                .ToArray()
            )
            .Transform(expr.Call)
            .Transform(s =>
            {
                //Add again X value after expression is applied
                var X = generationOperation.Result[0].signal.at(0);
                return np.concatenate([X,s]);
            });

        //this one applies filters/transformations/normalizations/etc
        var createdSignal = combineSources.Composition(
            [s=>s.at(1), // first select Y dimension
            // then apply filters,transforms,etc
            ..
            ops.Select(v=>(Func<ndarray,ndarray>)v.Compute)]
        );

        // Subscribe to the OnExecutedStep event to update completion percentage
        createdSignal.OnExecutedStep += (_) => {
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

        // if something broke
        createdSignal.OnException += e =>
        {
            var inner = e.InnerException ?? e;
            while(inner is AggregateException && inner.InnerException is not null)
                inner = inner.InnerException;
            System.Console.WriteLine(inner.Message);
        };

        // This event called once computation is completed
        createdSignal.OnExecutionDone+=res=>{
            var genOut = combineSources?.Result;
            System.Console.WriteLine("====================");
            System.Console.WriteLine(res.shape);
            System.Console.WriteLine(genOut?.shape);
            // if we got single signal output of shape [1,N]
            if(res.shape[0]==1 && res.shape.iDims.Length==2 || res.shape.iDims.Length==1)
            {
                if (res.Dtype == np.Complex)
                    _yImagValues=res.Imag?.AsFloatArray();
                else
                    _yImagValues=null;

                // here X is signal time, Y is signal value

                // Store the X and Y values for plotting
                _xValues = genOut?.at(0)?.AsFloatArray();
                _yValues = res.Real?.AsFloatArray();
                // Automatically plot as a line chart after computation is done
                // Need to dispatch to UI thread since this event is called from background thread
                Dispatcher.UIThread.Post(() => {
                    PlotLine();
                });
            }
        };
    }

    // Chart commands
    [RelayCommand]
    private void PlotLine()
    {
        if (_xValues != null && _yValues != null)
        {
            Series.Clear();
            Series.Add(
                RenderUtils.Plot("Real",_xValues, _yValues,color: SkiaSharp.SKColors.Blue)
            );
            if(_yImagValues is not null)
            {
                Series.Add(
                    RenderUtils.Plot("Imag",_xValues, _yImagValues,color: SkiaSharp.SKColors.Orange)
                );
            }

            // Update axes if needed
            XAxes = new List<Axis> { new Axis { Name = "Time" } };
            YAxes = new List<Axis> { new Axis { Name = "Amplitude" } };
        }
    }

    [RelayCommand]
    private void PlotScatter()
    {
        if (_xValues != null && _yValues != null)
        {
            Series.Clear();
            Series.Add(
                RenderUtils.Scatter("Real",_xValues, _yValues)
            );
            if(_yImagValues is not null)
            {
                Series.Add(
                    RenderUtils.Scatter("Imag",_xValues, _yImagValues)
                );
            }

            // Update axes if needed
            XAxes = new List<Axis> { new Axis { Name = "Time" } };
            YAxes = new List<Axis> { new Axis { Name = "Amplitude" } };
        }
    }

    [RelayCommand]
    private void ClearPlot()
    {
        Series.Clear();
    }

    [RelayCommand]
    private void ToggleFilterEnabled(FilterItemViewModel filter)
    {
        if (filter != null)
        {
            filter.Enabled = !filter.Enabled;
        }
    }
}
