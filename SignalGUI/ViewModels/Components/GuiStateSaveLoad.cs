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

/// <summary>
/// This class represents a signal GUI instance with all required fields,
/// so that we can have multiple instances of signal computation/renders
/// at the same time and we can switch signals view on GUI if we wish.
/// </summary>
public partial class GuiSignalInstance : ObservableObject
{
    [ObservableProperty]
    string _objectName = "";
    public required ComputeSignal? ComputeSignal;
    public required (string name, float stat)[]? SignalStatistics;
    public required int CompletedPercent;
    public required string Expression;
    public required int NextSourceLetterIndex;
    public required IEnumerable<GuiObjectFactory?> Sources;
    public required IEnumerable<(bool visible, GuiObjectFactory? factory)> Filters;
    public required GuiObjectFactory? SignalParams;

    // Chart properties
    public required IEnumerable<ISeries> Series;
    public required IEnumerable<Axis> XAxes;
    public required IEnumerable<Axis> YAxes;
    public required Bitmap? Image;
}
public partial class CompositeComponentViewModel : ViewModelBase
{
    /// <summary>
    /// Method to get snapshot of current GUI
    /// </summary>
    public GuiSignalInstance CreateGuiInstanceSnapshot()
    {
        return new()
        {
            ComputeSignal = _computeSignal?.Clone(),
            SignalStatistics =
                SignalStatistics?
                .Select(v=>(v.Name,v.Stat))
                .ToArray(),
            ObjectName = ObjectName,
            CompletedPercent = CompletedPercent,
            Expression = Expression,
            NextSourceLetterIndex = _nextSourceLetterIndex,
            Sources =
                Sources.Select(v=>v.Factory?.Clone()).ToArray(),
            Filters =
                Filters
                .Select(v=>(v.Enabled,v.Factory?.Clone())).ToArray(),
            SignalParams = SignalParams?.Clone(),
            Series = Series.ToArray(),
            XAxes = XAxes.ToArray(),
            YAxes = YAxes.ToArray(),
            Image=RenderedImage
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
        _computeSignal = instance.ComputeSignal;

        var stats = instance.SignalStatistics?.Select(v=> new SignalStatisticViewModel(v.name,v.stat)).ToArray();
        if(stats is not null)
            SignalStatistics = stats;
        ObjectName = instance.ObjectName;
        CompletedPercent = instance.CompletedPercent;
        Expression = instance.Expression;
        _nextSourceLetterIndex = instance.NextSourceLetterIndex;
        
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
        Series.AddRange(instance.Series);
        RenderedImage = instance.Image;

        XAxes = [.. instance.XAxes];
        YAxes = [.. instance.YAxes];
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
