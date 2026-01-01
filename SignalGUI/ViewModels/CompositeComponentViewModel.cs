using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignalCore;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _objectName = "NewObject";

    [ObservableProperty]
    private string _expression = "";
    private int _nextSourceLetterIndex = 0;
    public ObservableCollection<SourceItemViewModel> Sources { get; set; } = new();
    public ObservableCollection<FilterItemViewModel> Filters { get; set; } = new();
    
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

    [ObservableProperty]
    private SourceItemViewModel? _selectedSource;

    [ObservableProperty]
    private FilterItemViewModel? _selectedFilter;

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

    [ObservableProperty]
    private ObservableCollection<ParameterViewModelWithCallback> _currentParameters = new();

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
        // TODO: Implement the actual signal computation logic
        // This is a placeholder implementation
        Console.WriteLine($"Computing signal for object: {ObjectName}");
        Console.WriteLine($"Expression: {Expression}");

        // Here you would typically:
        // 1. Parse the expression
        // 2. Evaluate the signal based on sources and filters
        // 3. Apply the filters in sequence
        // 4. Store or display the result
    }
}

public partial class SourceItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _letter = "";

    [ObservableProperty]
    private GuiObjectFactory? _factory;

    public string Configuration => $"{Factory?.Name}";
}

public partial class FilterItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private GuiObjectFactory? _factory;
    public string Configuration => $"{Factory?.Name}";
}