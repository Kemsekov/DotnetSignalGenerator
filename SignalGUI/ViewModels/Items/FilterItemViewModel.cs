using CommunityToolkit.Mvvm.ComponentModel;
using SignalCore.Storage;

namespace SignalGUI.ViewModels;

public partial class FilterItemViewModel : ViewModelBase
{
    [ObservableProperty]
    ObjectFactory _factory = new(typeof(object),[]);

    [ObservableProperty]
    bool _enabled = true;
    public string Configuration => $"{Factory?.Type.Name}";
}