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
using DynamicData;

namespace SignalGUI.ViewModels;

/// <summary>
/// This class represents a signal GUI instance with all required fields,
/// so that we can have multiple instances of signal computation/renders 
/// at the same time and we can switch signals view on GUI if we wish.
/// </summary>
public class GuiSignalInstance
{
    public required ComputeSignal? ComputeSignal;
    public required SignalStatisticViewModel[]? SignalStatistics;
    public required string ObjectName;
    public required int CompletedPercent;
    public required string Expression;
    public required int NextSourceLetterIndex;
    public required IEnumerable<SourceItemViewModel> Sources;
    public required IEnumerable<FilterItemViewModel> Filters;
    public required GuiObjectFactory? SignalParams;

    // Chart properties
    public required IEnumerable<ISeries> Series;
    public required IEnumerable<Axis> XAxes;
    public required IEnumerable<Axis> YAxes;
    // Property to hold the rendered 2D image
    public required Bitmap? RenderedImage;
}

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
    /// <summary>
    /// Method to get snapshot of current GUI
    /// </summary>
    public GuiSignalInstance SaveGuiInstance()
    {
        return new()
        {
            ComputeSignal = _computeSignal,
            SignalStatistics = SignalStatistics,
            ObjectName = ObjectName,
            CompletedPercent = CompletedPercent,
            Expression = Expression,
            NextSourceLetterIndex = _nextSourceLetterIndex,
            Sources = Sources,
            Filters = Filters,
            SignalParams = SignalParams,
            Series = Series,
            XAxes = XAxes,
            YAxes = YAxes,
            RenderedImage = RenderedImage
        };
    }
    /// <summary>
    /// Method to load snapshot of current GUI
    /// </summary>
    public void LoadGuiInstance(GuiSignalInstance instance)
    {
        _computeSignal = instance.ComputeSignal;
        SignalStatistics = instance.SignalStatistics;
        ObjectName = instance.ObjectName;
        CompletedPercent = instance.CompletedPercent;
        Expression = instance.Expression;
        _nextSourceLetterIndex = instance.NextSourceLetterIndex;
        Sources.Clear();
        Sources.AddRange(instance.Sources);
        Filters.Clear();
        Filters.AddRange(instance.Filters);
        SignalParams = instance.SignalParams;
        Series.Clear();
        Series.AddRange(instance.Series);
        XAxes = [.. instance.XAxes];
        YAxes = [.. instance.YAxes];
        RenderedImage = instance.RenderedImage;
    }

    ComputeSignal? _computeSignal;
    [ObservableProperty]
    SignalStatisticViewModel[]? _signalStatistics;

    [ObservableProperty]
    private string _objectName = "NewObject";

    [ObservableProperty]
    private int _completedPercent = 0;

    [ObservableProperty]
    private string _expression = "";
    private int _nextSourceLetterIndex = 0;
    public ObservableCollection<SourceItemViewModel> Sources { get; set; } = new();
    public ObservableCollection<FilterItemViewModel> Filters { get; set; } = new();
    public List<string> AvailableSourcesForExpression => Sources.Select(s => s.Letter).ToList();
    [ObservableProperty]
    private GuiObjectFactory? _signalParams = SignalParameters.CreateFactory();

    public SignalParameters SignalParameters => SignalParams?.GetInstance() as SignalParameters ?? 
                throw new ArgumentException("Failed to cast SignalParameters");
    // Chart properties
    [ObservableProperty]
    private ObservableCollection<ISeries> _series = new();

    [ObservableProperty]
    private List<Axis> _xAxes = new() { new Axis { Name = "Time" } };

    [ObservableProperty]
    private List<Axis> _yAxes = new() { new Axis { Name = "Amplitude" } };
    
    // Property to hold the rendered 2D image
    [ObservableProperty]
    private Bitmap? _renderedImage;
}
