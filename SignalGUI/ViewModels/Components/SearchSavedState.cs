using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel : ViewModelBase
{
    [ObservableProperty]
    string _searchText = "";

    // Filtered collection for displaying search results
    public ObservableCollection<GuiSignalInstance> FilteredGuiInstances { get; set; } = new();

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilteredGuiInstances();
    }

    void UpdateFilteredGuiInstances()
    {
        FilteredGuiInstances.Clear();
        
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // If search text is empty, show all instances
            foreach (var instance in SavedGuiInstances)
            {
                FilteredGuiInstances.Add(instance);
            }
        }
        else
        {
            // Filter instances based on the search text
            var searchTextLower = SearchText.ToLower();
            foreach (var instance in SavedGuiInstances)
            {
                if (instance.ObjectName != null && 
                    instance.ObjectName.ToLower().Contains(searchTextLower))
                {
                    FilteredGuiInstances.Add(instance);
                }
            }
        }
    }

    // Initialize the filtered collection when the ViewModel is created
    void InitializeSearchFunctionality()
    {
        // Subscribe to the collection changed event to update filtered list when items are added/removed
        SavedGuiInstances.CollectionChanged += (sender, e) =>
        {
            UpdateFilteredGuiInstances();
        };

        // Initialize the filtered collection with all saved instances
        UpdateFilteredGuiInstances();
    }
}