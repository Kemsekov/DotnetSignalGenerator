using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalGUI.ViewModels;

public partial class SourceItemViewModel : ViewModelBase
{
    [ObservableProperty]
    string _letter = "";

    [ObservableProperty]
    GuiObjectFactory? _factory;

    public string Configuration => $"{Factory?.Name}";
}
