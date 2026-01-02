using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalGUI.ViewModels;

public partial class SourceItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _letter = "";

    [ObservableProperty]
    private GuiObjectFactory? _factory;

    public string Configuration => $"{Factory?.Name}";
}
