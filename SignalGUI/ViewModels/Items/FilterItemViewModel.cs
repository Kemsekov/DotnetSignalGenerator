using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalGUI.ViewModels;

public partial class FilterItemViewModel : ViewModelBase
{
    [ObservableProperty]
    GuiObjectFactory? _factory;

    [ObservableProperty]
    bool _enabled = true;

    public string Configuration => $"{Factory?.Name}";
}