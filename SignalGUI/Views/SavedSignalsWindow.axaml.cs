using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SignalGUI.ViewModels;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace SignalGUI.Views;

public partial class SavedSignalsWindow : Window
{
    CompositeComponentViewModel? _viewModel;

    public SavedSignalsWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public SavedSignalsWindow(CompositeComponentViewModel viewModel) : this()
    {
        DataContext = viewModel;
        _viewModel = viewModel;
        _viewModel.CloseSavedSignalsWindowAction = () => Close();

        // Debug output to see what's in the collection when the window is created
        System.Console.WriteLine($"SavedSignalsWindow created. ViewModel SavedGuiInstances count: {_viewModel?.SavedGuiInstances?.Count ?? -1}");
        if (_viewModel?.SavedGuiInstances != null)
        {
            foreach (var item in _viewModel.SavedGuiInstances)
            {
                System.Console.WriteLine($"  - Item ObjectName: {item?.ObjectName}");
            }
        }
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

}