using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SignalGUI.Views;

public partial class ErrorWindow : Window
{
    TextBlock _errorMessageTextBlock;
    Button _okButton;

    public ErrorWindow()
    {
        InitializeComponent();
        _errorMessageTextBlock = this.FindControl<TextBlock>("ErrorMessageTextBlock") ?? throw new KeyNotFoundException("ErrorMessageTextBlock");
        _okButton = this.FindControl<Button>("OkButton")  ?? throw new KeyNotFoundException("OkButton");

#if DEBUG
        this.AttachDevTools();
#endif
    }

    public ErrorWindow(string errorMessage) : this()
    {
        _errorMessageTextBlock.Text = errorMessage;
        _okButton.Click += (sender, e) => Close();
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}