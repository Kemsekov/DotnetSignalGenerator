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
using SignalCore.GuiState;

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

    // Core GUI state that contains all the important data except the GUI-specific properties
    public GuiSignalState CoreState { get; set; }

    public GuiSignalInstance(string objectName)
    {
        _objectName = objectName;
        CoreState = new GuiSignalState(objectName);
        PropertyChanged += (sender, info) =>
        {
            if((info.PropertyName ?? "").Contains("Name"))
                CoreState.ObjectName=ObjectName;
        };
    }

    public void LoadFromDB(SignalStorage signalStorage)
    {
        var loadedState = GuiStateConverter.LoadFromDB(signalStorage, ObjectName);
        CoreState = loadedState;
        ObjectName = loadedState.ObjectName;
        //  = loadedState.SignalParams;
    }

    public SessionStateModel ToSessionStateModel()
    {
        var stateForConversion = new GuiSignalState(
            ObjectName,
            CoreState.ComputedSignal,
            CoreState.SignalStatistics,
            CoreState.CompletedPercent,
            CoreState.Sources,
            CoreState.Expression,
            CoreState.Filters,
            CoreState.SignalParams
        );

        return GuiStateConverter.ToSessionStateModel(stateForConversion);
    }
   
}
public partial class CompositeComponentViewModel : ViewModelBase
{
    public void LoadSessionsFromDB()
    {
        var newSet = SessionStorage.db
        .Table<SessionModel>()
        .Select(v=> new GuiSignalInstance(v.Name))
        .ToArray();
        SavedGuiInstances.Clear();
        SavedGuiInstances.AddRange(newSet);
    }
    /// <summary>
    /// Method to get snapshot of current GUI
    /// </summary>
    public GuiSignalInstance CreateGuiInstanceSnapshot()
    {
        var snapshot = new GuiSignalInstance(ObjectName);
        snapshot.CoreState = new GuiSignalState(
            ObjectName,
            _computedSignal?.Clone(),
            SignalStatistics?.Select(v => (v.Name, v.Stat)).ToArray(),
            CompletedPercent,
            Sources.Select(v => v.Factory.Clone()).ToArray(),
            Expression,
            Filters.Select(v => (v.Enabled, v.Factory.Clone())).ToArray(),
            SignalParams?.Clone()
        );
        return snapshot;
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
        
        var state = instance.CoreState;

        _computedSignal = state.ComputedSignal;

        var stats = state.SignalStatistics?.Select(v=> new SignalStatisticViewModel(v.name,v.stat)).ToArray();
        if(stats is not null)
            SignalStatistics = stats;
        ObjectName = instance.ObjectName;
        CompletedPercent = state.CompletedPercent;
        Expression = state.Expression;

        Sources.Clear();
        Sources.AddRange(state.Sources.Select(v=>new SourceItemViewModel
        {
            Factory=v
        }));

        Filters.Clear();
        Filters.AddRange(state.Filters.Select(v=>new FilterItemViewModel
        {
            Enabled=v.visible,
            Factory=v.factory
        }));
        ReassignSourceLetters();
        CurrentParameters = new();

        SignalParams = state.SignalParams;

        System.Console.WriteLine("SIGNAL PARAMS");
        System.Console.WriteLine(state.SignalParams?.ToJson());

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
                System.Console.WriteLine("Removing instance with name",dbSessionInstance.Name);
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
