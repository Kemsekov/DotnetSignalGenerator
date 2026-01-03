using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SignalGUI.Views;

public partial class ErrorWindow : Window
{
    private TextBlock _errorMessageTextBlock;
    private Button _okButton;

    public ErrorWindow()
    {
        InitializeComponent();
        _errorMessageTextBlock = this.FindControl<TextBlock>("ErrorMessageTextBlock");
        _okButton = this.FindControl<Button>("OkButton");

#if DEBUG
        this.AttachDevTools();
#endif
    }

    public ErrorWindow(string errorMessage) : this()
    {
        _errorMessageTextBlock.Text = errorMessage;
        _okButton.Click += (sender, e) => Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}