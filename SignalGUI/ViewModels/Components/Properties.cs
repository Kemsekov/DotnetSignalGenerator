using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignalCore;
using SignalGUI.Utils;
using NumpyDotNet;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Avalonia.Media.Imaging;
using DynamicData;

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
    SourceItemViewModel? _selectedSource;

    [ObservableProperty]
    FilterItemViewModel? _selectedFilter;
    [ObservableProperty]
    ObservableCollection<ParameterViewModelWithCallback> _currentParameters = new();

    ComputeSignal? _computeSignal;
    [ObservableProperty]
    SignalStatisticViewModel[]? _signalStatistics;

    [ObservableProperty]
    string _objectName = "NewObject";

    [ObservableProperty]
    int _completedPercent = 0;

    [ObservableProperty]
    string _expression = "";
    int _nextSourceLetterIndex = 0;
    public ObservableCollection<SourceItemViewModel> Sources { get; set; } = new();
    public ObservableCollection<FilterItemViewModel> Filters { get; set; } = new();
    public List<string> AvailableSourcesForExpression => Sources.Select(s => s.Letter).ToList();
    [ObservableProperty]
    GuiObjectFactory? _signalParams = SignalParameters.CreateFactory();

    public SignalParameters SignalParameters => SignalParams?.GetInstance() as SignalParameters ?? 
                throw new ArgumentException("Failed to cast SignalParameters");
    // Chart properties
    [ObservableProperty]
    ObservableCollection<ISeries> _series = new();

    [ObservableProperty]
    List<Axis> _xAxes = new() { new Axis { Name = "Time" } };

    [ObservableProperty]
    List<Axis> _yAxes = new() { new Axis { Name = "Amplitude" } };
    
    // Property to hold the rendered 2D image
    [ObservableProperty]
    Bitmap? _renderedImage;

    // Collection to store saved GUI instances
    public ObservableCollection<GuiSignalInstance> SavedGuiInstances { get; set; } = new();
    
    // Command to save the current GUI instance
    [ObservableProperty]
    ICommand? _saveGuiInstanceCommand;

    // Command to load a selected GUI instance
    [ObservableProperty]
    ICommand? _loadGuiInstanceCommand;

    // Property to track the selected GUI instance from the dropdown
    [ObservableProperty]
    GuiSignalInstance? _selectedGuiInstance;
}
