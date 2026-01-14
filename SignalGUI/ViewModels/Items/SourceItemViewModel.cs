using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using SignalCore.Storage;

namespace SignalGUI.ViewModels;

public partial class SourceItemViewModel : ViewModelBase
{
    [ObservableProperty]
    string _letter = "";

    [ObservableProperty]
    ObjectFactory _factory = new(typeof(object),[]);
    public string Configuration => $"{Factory?.Type.Name}";
}
