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
            .Select(v=>(v.visible,new GuiObjectFactory(v.factory)))
            .ToArray() ?? throw new Exception();

        ObjectName=s.Name;
        ComputedSignal=computedSignal;
        Expression=s.Expression;
        Filters=ops;
        SignalParams = new(new ObjectFactory(
            typeof(SignalParameters),
            [
                ("computePoints",s.ComputePoints),
                ("renderPoints",256)
            ]
        ));
        SignalStatistics=s.SignalStatistics.Select(v=>(v.Name,v.Statistic)).ToArray();
        Sources=generations.Select(v=>new GuiObjectFactory(v.factory)).ToArray();
    }
    
    public required ComputedSignal? ComputedSignal=null;
    public required (string name, float stat)[]? SignalStatistics;
    public required int CompletedPercent=0;
    public required IEnumerable<GuiObjectFactory> Sources=[];
    public required string Expression="";
    public required IEnumerable<(bool visible, GuiObjectFactory factory)> Filters=[];
    public required GuiObjectFactory? SignalParams=null;
}
public partial class CompositeComponentViewModel : ViewModelBase
{
    public void LoadSessionsFromDB()
    {
        
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
            System.Console.WriteLine($"Created instance with ObjectName: {instance.ObjectName}");
            instance.ObjectName = name; // Update the name to the provided one
            System.Console.WriteLine($"Set instance ObjectName to: {instance.ObjectName}");

            // Check if an instance with the same name already exists
            var existingIndex = -1;
            for (int i = 0; i < SavedGuiInstances.Count; i++)
            {
                System.Console.WriteLine($"Checking existing instance {i}: {SavedGuiInstances[i].ObjectName}");
                if (SavedGuiInstances[i].ObjectName?.Equals(name) == true)
                {
                    existingIndex = i;
                    System.Console.WriteLine($"Found existing instance at index: {existingIndex}");
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                // Replace the existing instance
                SavedGuiInstances[existingIndex] = instance;
                System.Console.WriteLine($"Replaced instance at index: {existingIndex}");
            }
            else
            {
                // Add the new instance
                SavedGuiInstances.Add(instance);
                System.Console.WriteLine($"Added new instance. Total count: {SavedGuiInstances.Count}");
            }
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
        System.Console.WriteLine($"RemoveGuiInstance called with instance: {(instance?.ObjectName ?? "null")}");
        if (instance != null)
        {
            System.Console.WriteLine($"Removing instance with ObjectName: {instance.ObjectName}. Before removal count: {SavedGuiInstances.Count}");
            SavedGuiInstances.Remove(instance);
            System.Console.WriteLine($"After removal count: {SavedGuiInstances.Count}");
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
