using CommunityToolkit.Mvvm.Input;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel
{
    [RelayCommand]
    public void AddSources(GuiObjectFactory selectedSourceType)
    {
        
        if (selectedSourceType == null) return;

        var letter = GetNextLetter();
        var sourceItem = new SourceItemViewModel
        {
            Letter = letter,
            Factory = selectedSourceType.Clone() // Store the factory instead of a string
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
    public void AddFilters(GuiObjectFactory selectedFilterType)
    {
        if (selectedFilterType == null) return;

        var filterItem = new FilterItemViewModel
        {
            Factory = selectedFilterType.Clone() // Store the factory instead of a string
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
}