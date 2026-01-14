using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
using SignalCore.Storage;
using SQLiteNetExtensions.Extensions;

namespace SignalGUI.ViewModels;

/// <summary>
/// This class represents a signal GUI instance with all required fields,
/// so that we can have multiple instances of signal computation/renders
/// at the same time and we can switch signals view on GUI if we wish.
/// </summary>
public partial class GuiSignalInstance : ObservableObject
{
    [ObservableProperty]
    string _objectName = "";

    public GuiSignalInstance(string objectName)
    {
        _objectName=objectName;
    }

    public void LoadFromDB(SignalStorage signalStorage)
    {
        var s = signalStorage.db.Table<SessionModel>().FirstOrDefault(v=>v.Name== ObjectName);
        if(s is null)
            throw new KeyNotFoundException($"Cannot find database signal instance with name {ObjectName}");
        
        s = signalStorage.db.GetWithChildren<SessionModel>(s.Id);
        var sFields = signalStorage.GetSessionState(s.Id);
        // s.Signal.GetNdarray
        var computedSignal = new ComputedSignal(
            s.SignalX.GetNdarray(),
            s.SignalY.GetNdarray(),
            s.SignalStatistics.Select(v=>(v.Statistic,v.Name)).ToArray()
        );
        var filters = sFields.Filters.Select(v=>(
            visible: v.Enabled,
            name:v.VarName,
            factory:v.Filter.Factory
        ));
        var transforms = sFields.Transforms.Select(v=>(
            visible: v.Enabled,
            name:v.VarName,
            factory:v.Transform.Factory
        ));

        var generations = sFields.Generations.Select(v=>(
            name:v.VarName,
            factory:v.Generation.Factory
        ));

        var norms = sFields.Normalizations.Select(v=>(
            visible: v.Enabled,
            name:v.VarName,
            factory:v.Normalization.Factory
        ));

        var ops = 
            new[]{filters,transforms,norms}
            .SelectMany(v=>v)
            .OrderBy(v=>v.name)
            .Select(v=>(v.visible,v.factory))
            .ToArray() ?? throw new Exception();

        ObjectName=s.Name;
        ComputedSignal=computedSignal;
        Expression=s.Expression;
        Filters=ops;
        SignalParams = new ObjectFactory(
            typeof(SignalParameters),
            args: [
                ("computePoints",s.ComputePoints),
                ("renderPoints",256)
            ]
        );
        SignalStatistics=s.SignalStatistics.Select(v=>(v.Name,v.Statistic)).ToArray();
        Sources=generations.Select(v=>v.factory).ToArray();
    }
    public SessionStateModel ToSessionStateModel()
    {
        // Create the main session model
        var sessionModel = new SessionModel
        {
            Name = ObjectName,
            Expression = Expression,
            ComputePoints = SignalParams?.ConstructorArguments.ContainsKey("computePoints") ?? false
                ? Convert.ToInt32(SignalParams?.ConstructorArguments["computePoints"].Instance ?? 1024)
                : 1024,
            SignalX = new NDarrayBinaryDataModel(),
            SignalY = new NDarrayBinaryDataModel(),
            SignalStatistics = SignalStatistics?.Select(v => new SignalStatistic { Name = v.name, Statistic = v.stat }).ToList() ?? []
        };

        sessionModel.SignalX.SetNdarray(0.ToNdarray());
        sessionModel.SignalY.SetNdarray(0.ToNdarray());

        // Set signal data if ComputedSignal is available
        if (ComputedSignal != null && ComputedSignal.X != null && ComputedSignal.Y != null)
        {
            var xArray = np.array(ComputedSignal.X, np.Float32);
            // For complex signals, we need to handle the YImag part
            ndarray yArray;
            if (ComputedSignal.YImag != null)
            {
                // Create complex array from real and imaginary parts
                var complexValues = new System.Numerics.Complex[ComputedSignal.Y.Length];
                for (int i = 0; i < ComputedSignal.Y.Length; i++)
                {
                    complexValues[i] = new System.Numerics.Complex(ComputedSignal.Y[i], ComputedSignal.YImag[i]);
                }
                yArray = np.array(complexValues, np.Complex);
            }
            else
            {
                yArray = np.array(ComputedSignal.Y, np.Float32);
            }

            sessionModel.SignalX.SetNdarray(xArray);
            sessionModel.SignalY.SetNdarray(yArray);
        }
        var imd = this.ComputedSignal?.ImageData;
        if(imd is not null)
            sessionModel.SignalY.SetNdarray(imd);

        // Create relation models for generations (sources)
        var generations = Sources?.Select((factory, index) => new SessionGenerators
        {
            Session=sessionModel,
            VarName = $"source_{index}",
            Generation = new GenerationModel
            {
                Factory = factory
            }
        }).ToArray() ?? Array.Empty<SessionGenerators>();

        // Separate the mixed operations (filters, transforms, normalizations) from the Filters property
        var allOperations = 
            Filters?.Select((v,ind)=>(op:v,ind:ind))
            ?? [];

        var extractedFilters = allOperations
        .Where(v=>IsFilterOperation(v.op))
        .Select(opWithInd => new SessionFilters
        {
            Session=sessionModel,
            VarName = $"operation_{opWithInd.ind}",
            Enabled = opWithInd.op.visible,
            Filter = new FilterModel
            {
                Factory = opWithInd.op.factory
            }
        }).ToArray();

        var extractedTransforms = allOperations.Where(v=>IsTransformOperation(v.op)).Select(v => new SessionTransforms
        {
            Session=sessionModel,
            VarName = $"operation_{v.ind}",
            Enabled = v.op.visible,
            Transform = new TransformModel
            {
                Factory = v.op.factory
            }
        }).ToArray();

        var extractedNorms = allOperations.Where(v=>IsNormalizationOperation(v.op))
        .Select(v => new SessionNormalization
        {
            Session=sessionModel,
            VarName = $"operation_{v.ind}",
            Enabled = v.op.visible,
            Normalization = new NormalizationModel
            {
                Factory = v.op.factory
            }
        }).ToArray();

        return new SessionStateModel(
            sessionModel,
            generations,
            extractedFilters,
            extractedTransforms,
            extractedNorms
        );
    }

    // Helper methods to determine the type of operation
    private bool IsFilterOperation((bool visible, ObjectFactory factory) op)
    {
        // Check if the factory's object type is related to filtering
        // This could be based on interface implementation or naming convention
        return op.factory.Type.Name.ToLower().Contains("filter");
    }

    private bool IsTransformOperation((bool visible, ObjectFactory factory) op)
    {
        // Check if the factory's object type is related to transformation
        return op.factory.Type.Name.ToLower().Contains("transform");
    }

    private bool IsNormalizationOperation((bool visible, ObjectFactory factory) op)
    {
        // Check if the factory's object type is related to normalization
        return op.factory.Type.Name.ToLower().Contains("normalize");
    }

    public ComputedSignal? ComputedSignal=null;
    public (string name, float stat)[]? SignalStatistics;
    public int CompletedPercent=0;
    public IEnumerable<ObjectFactory> Sources=[];
    public string Expression="";
    public IEnumerable<(bool visible, ObjectFactory factory)> Filters=[];
    public ObjectFactory? SignalParams=null;
}
public partial class CompositeComponentViewModel : ViewModelBase
{
    public void LoadSessionsFromDB()
    {

        var newSet = SessionStorage.db
        .Table<SessionModel>()
        .Select(v=>
            new GuiSignalInstance(v.Name))
        .ToArray();
        SavedGuiInstances.Clear();
        SavedGuiInstances.AddRange(newSet);
    }
    /// <summary>
    /// Method to get snapshot of current GUI
    /// </summary>
    public GuiSignalInstance CreateGuiInstanceSnapshot()
    {
        return new(ObjectName)
        {
            ComputedSignal = _computedSignal?.Clone(),
            SignalStatistics =
                SignalStatistics?
                .Select(v=>(v.Name,v.Stat))
                .ToArray(),
            ObjectName = ObjectName,
            CompletedPercent = CompletedPercent,
            Expression = Expression,
            Sources =
                Sources.Select(v=>v.Factory.Clone()).ToArray(),
            Filters =
                Filters
                .Select(v=>(v.Enabled,v.Factory.Clone())).ToArray(),
            SignalParams = SignalParams?.Clone(),
        };
    }

    /// <summary>
    /// Method to save current GUI state with a name
    /// </summary>
    public void SaveGuiInstance(string name)
    {
        try
        {
            System.Console.WriteLine($"Inside SaveGuiInstance with name: {name}");
            var instance = CreateGuiInstanceSnapshot();
            
            //remove all related to saved instance
            RemoveGuiInstance(instance);

            System.Console.WriteLine($"Created instance with ObjectName: {instance.ObjectName}");
            instance.ObjectName = name; // Update the name to the provided one
            System.Console.WriteLine($"Set instance ObjectName to: {instance.ObjectName}");

            //Save current state to DB
            var state = instance.ToSessionStateModel();
            SessionStorage.AddSessionState(state);
            
            //Reload all sessions from DB
            LoadSessionsFromDB();
        }
        catch (Exception ex)
        {
            // In a real application, you'd want to log this properly
            System.Console.WriteLine($"Error in SaveGuiInstance: {ex.Message}");
        }
    }

    /// <summary>
    /// Method to load snapshot of current GUI
    /// </summary>
    public void LoadGuiInstance(GuiSignalInstance instance)
    {
        this.RenderedImage = null;
        instance.LoadFromDB(SessionStorage);
        _computedSignal = instance.ComputedSignal;

        var stats = instance.SignalStatistics?.Select(v=> new SignalStatisticViewModel(v.name,v.stat)).ToArray();
        if(stats is not null)
            SignalStatistics = stats;
        ObjectName = instance.ObjectName;
        CompletedPercent = instance.CompletedPercent;
        Expression = instance.Expression;
        
        Sources.Clear();
        Sources.AddRange(instance.Sources.Select(v=>new SourceItemViewModel
        {
            Factory=v
        }));

        Filters.Clear();
        Filters.AddRange(instance.Filters.Select(v=>new FilterItemViewModel
        {
            Enabled=v.visible,
            Factory=v.factory
        }));
        ReassignSourceLetters();
        CurrentParameters = new();

        SignalParams = instance.SignalParams;
        Series.Clear();
        PlotLine();
        Plot2DImage();
    }

    public ICommand? ShowSavedSignalsCommand { get; set; }
    public ICommand? LoadSpecificGuiInstanceCommand { get; set; }
    public ICommand? RemoveGuiInstanceCommand { get; set; }
    public Action? ShowSavedSignalsAction { get; set; }
    public Action? CloseSavedSignalsWindowAction { get; set; }

    void ShowSavedSignals()
    {
        System.Console.WriteLine($"ShowSavedSignals called. SavedGuiInstances count: {SavedGuiInstances.Count}");
        LoadSessionsFromDB();
        ShowSavedSignalsAction?.Invoke();
    }

    void LoadSpecificGuiInstance(GuiSignalInstance? instance)
    {
        System.Console.WriteLine($"LoadSpecificGuiInstance called with instance: {(instance?.ObjectName ?? "null")}");
        if (instance != null)
        {
            System.Console.WriteLine($"Loading instance with ObjectName: {instance.ObjectName}");
            LoadGuiInstance(instance);
        }
    }

    void RemoveGuiInstance(GuiSignalInstance? instance)
    {
        if(instance is not null)
        {
            var dbSessionInstance = SessionStorage.db.Table<SessionModel>().FirstOrDefault(v=>v.Name==instance.ObjectName);
            if(dbSessionInstance is not null)
            {
                SessionStorage.DeleteSession(dbSessionInstance.Id);
            }
            LoadSessionsFromDB();
        }
    }

    void SaveCurrentGuiInstance()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(ObjectName) ? "Unnamed" : ObjectName;
            System.Console.WriteLine($"Attempting to save GUI instance with name: {name}");
            System.Console.WriteLine($"Current SavedGuiInstances count: {SavedGuiInstances.Count}");

            SaveGuiInstance(name); // Use the current ObjectName as the instance name

            System.Console.WriteLine($"After save, SavedGuiInstances count: {SavedGuiInstances.Count}");
        }
        catch (Exception ex)
        {
            // In a real application, you'd want to log this properly
            System.Console.WriteLine($"Error saving GUI instance: {ex.Message}");
        }
    }

    void LoadSelectedGuiInstance()
    {
        if (SelectedGuiInstance != null)
        {
            LoadGuiInstance(SelectedGuiInstance);
        }
    }
    // Initialize the commands in the constructor
    public CompositeComponentViewModel()
    {
        SaveGuiInstanceCommand = new RelayCommand(SaveCurrentGuiInstance);
        LoadGuiInstanceCommand = new RelayCommand(LoadSelectedGuiInstance);
        ShowSavedSignalsCommand = new RelayCommand(ShowSavedSignals);
        LoadSpecificGuiInstanceCommand = new RelayCommand<GuiSignalInstance>(LoadSpecificGuiInstance);
        RemoveGuiInstanceCommand = new RelayCommand<GuiSignalInstance>(RemoveGuiInstance);

        // Initialize search functionality
        InitializeSearchFunctionality();
    }
}
