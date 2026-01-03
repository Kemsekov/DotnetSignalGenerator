using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignalCore;
using SignalGUI.Utils;
using SignalCore.Computation;
using NumpyDotNet;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Avalonia.Threading;
using SkiaSharp;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel
{
    // Chart commands
    [RelayCommand]
    private void PlotLine()
    {
        if (_xValues != null && _yValues != null)
        {
            Series.Clear();
            Series.Add(
                RenderUtils.Plot("Real",_xValues, _yValues,color: SKColors.Blue)
            );
            if(_yImagValues is not null)
            {
                Series.Add(
                    RenderUtils.Plot("Imag",_xValues, _yImagValues,color: SKColors.Orange)
                );
            }

            // Update axes if needed
            XAxes = new List<Axis> { new Axis { Name = "Time" } };
            YAxes = new List<Axis> { new Axis { Name = "Amplitude" } };
        }
    }

    [RelayCommand]
    private void PlotScatter()
    {
        if (_xValues != null && _yValues != null)
        {
            Series.Clear();
            Series.Add(
                RenderUtils.Scatter("Real",_xValues, _yValues)
            );
            if(_yImagValues is not null)
            {
                Series.Add(
                    RenderUtils.Scatter("Imag",_xValues, _yImagValues)
                );
            }

            // Update axes if needed
            XAxes = new List<Axis> { new Axis { Name = "Time" } };
            YAxes = new List<Axis> { new Axis { Name = "Amplitude" } };
        }
    }

    [RelayCommand]
    private void ClearPlot()
    {
        Series.Clear();
    }

    [RelayCommand]
    private void ToggleFilterEnabled(FilterItemViewModel filter)
    {
        if (filter != null)
        {
            filter.Enabled = !filter.Enabled;
        }
    }
}