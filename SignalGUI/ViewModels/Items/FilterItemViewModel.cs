using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalGUI.ViewModels;

public partial class FilterItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private GuiObjectFactory? _factory;

    [ObservableProperty]
    private bool _enabled = true;

    public string Configuration => $"{Factory?.Name}";
}