using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SignalCore;
using SignalGUI.Utils;
using NumpyDotNet;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Avalonia.Media.Imaging;

namespace SignalGUI.ViewModels;


public partial class CompositeComponentViewModel : ViewModelBase
{
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

    [ObservableProperty]
    private SourceItemViewModel? _selectedSource;

    [ObservableProperty]
    private FilterItemViewModel? _selectedFilter;
        [ObservableProperty]
    private ObservableCollection<ParameterViewModelWithCallback> _currentParameters = new();

    // --------------------
    ComputeSignal? _computeSignal;
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
    private GuiObjectFactory? _signalParams = SignalParameters.CreateFactory();

    public SignalParameters SignalParameters => SignalParams?.GetInstance() as SignalParameters ?? 
                throw new ArgumentException("Failed to cast SignalParameters");

    public List<string> AvailableSourcesForExpression => Sources.Select(s => s.Letter).ToList();

    // Chart properties
    [ObservableProperty]
    private ObservableCollection<ISeries> _series = new();

    [ObservableProperty]
    private List<Axis> _xAxes = new() { new Axis { Name = "Time" } };

    [ObservableProperty]
    private List<Axis> _yAxes = new() { new Axis { Name = "Amplitude" } };


    // Property to hold the rendered 2D image
    private Bitmap? _renderedImage;
    public Bitmap? RenderedImage
    {
        get => _renderedImage;
        private set => SetProperty(ref _renderedImage, value);
    }
}
