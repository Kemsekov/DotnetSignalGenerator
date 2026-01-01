
namespace SignalGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    public CompositeComponentViewModel CompositeComponentViewModel { get; set; } = new();

}
