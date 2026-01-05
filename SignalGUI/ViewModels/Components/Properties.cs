using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SignalCore;
using SignalGUI.Utils;
using SignalCore.Computation;
using NumpyDotNet;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel : ViewModelBase
{
    TrackedOperation<ndarray>? _createdSignal;
    SignalStatisticViewModel[]? _signalStatistics;

    [ObservableProperty]
    private string _objectName = "NewObject";

    public SignalStatisticViewModel[]? SignalStatistics
    {
        get => _signalStatistics;
        private set => SetProperty(ref _signalStatistics, value);
    }

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

    public SignalParameters SignalParameters => SignalParams?.GetInstance() as SignalParameters ?? 
                throw new ArgumentException("Failed to cast SignalParameters");

    static List<GuiObjectFactory> _GetImplementationFactories(params Type[] t)
    {
        
        return t.SelectMany(v=>v.GetAllImplementations())
        .Select(type=>
            new{type, ctor=type.GetSupportedConstructor(ArgumentsTypesUtils.SupportedTypes)}
        )
        .Where(v=>v.ctor is not null)
        .Select(v=>new GuiObjectFactory(v.type,v.ctor ?? throw new Exception()))
        .ToList();
    }

    public List<ISignalStatistic> AvailableSignalStatistics { get; set; }
        = _GetImplementationFactories(typeof(ISignalStatistic))
        .Select(v=>(ISignalStatistic)v.GetInstance())
        .ToList();
    
    public List<GuiObjectFactory> AvailableSourceTypes { get; set; }
        = _GetImplementationFactories(typeof(ISignalGenerator));

    public List<GuiObjectFactory> AvailableFilterTypes { get; set; }
        = _GetImplementationFactories(typeof(IFilter),typeof(INormalization),typeof(ITransform));

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
}

public partial class SignalStatisticViewModel : ObservableObject
{
    public SignalStatisticViewModel(string name, float stat)
    {
        Name = name;
        Stat = stat;
    }

    public string Name { get; set; }
    public float Stat { get; set; }
}