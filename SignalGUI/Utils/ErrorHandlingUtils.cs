using System;
using Avalonia.Controls;
using SignalCore;
using SignalGUI.Views;

namespace SignalGUI.Utils;

public static class ErrorHandlingUtils
{
    public static void ShowErrorWindow(Exception exception, Window? owner = null)
    {
        var innerException = exception.GetMostInnerException();
        var memName = innerException.TargetSite?.MemberType.ToString().Split(".")[^1];
        var className = innerException.TargetSite?.DeclaringType?.ToString().Split(".")[^1];
        var errorMessage = $"{className}:{memName}\n{innerException.Message}";

        var errorWindow = new ErrorWindow(errorMessage);
        if (owner != null)
        {
            errorWindow.ShowDialog(owner);
        }
        else
        {
            errorWindow.Show();
        }
    }
}