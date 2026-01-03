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
}