using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalGUI.ViewModels;

public partial class SourceItemViewModel : ViewModelBase
{
    [ObservableProperty]
    string _letter = "";

    [ObservableProperty]
    GuiObjectFactory _factory = new(typeof(object),null);
    public string Configuration => $"{Factory?.Name}";
}
