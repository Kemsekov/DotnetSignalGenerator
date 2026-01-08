using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalGUI.ViewModels;

public partial class SignalStatisticViewModel : ObservableObject
{
    public SignalStatisticViewModel(string name, float stat)
    {
        Name = name;
        Stat = stat;
    }

    public string Name { get; set; }
    public float Stat { get; set; }
}