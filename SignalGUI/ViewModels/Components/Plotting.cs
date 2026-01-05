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
using NWaves.Filters.Bessel;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel
{
    // Chart commands
    [RelayCommand]
    private void PlotLine()
    {
        if (_xValues != null && _yValues != null)
        {
            
            var xmax = _xValues.Max();
            var xmin = _xValues.Min();
            double r = 0;
            var interpolateX = np.linspace(xmin,xmax,ref r,SignalParameters.RenderPoints);
            var x = np.array(_xValues,copy:false);
            var y = np.array(_yValues,copy:false);

            var interpolateY =  np.interp(interpolateX,x,y);

            Series.Clear();
            Series.Add(
                RenderUtils.Plot("Real",interpolateX.AsFloatArray(), interpolateY.AsFloatArray(),color: SKColors.Blue)
            );
            if(_yImagValues is not null)
            {
                var yImag = np.array(_yImagValues,copy:false);
                Series.Add(
                    RenderUtils.Plot("Imag",interpolateX.AsFloatArray(), yImag.AsFloatArray(),color: SKColors.Orange)
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
            
            var xmax = _xValues.Max();
            var xmin = _xValues.Min();
            double r = 0;
            var interpolateX = np.linspace(xmin,xmax,ref r,SignalParameters.RenderPoints);
            var x = np.array(_xValues,copy:false);
            var y = np.array(_yValues,copy:false);

            var interpolateY =  np.interp(interpolateX,x,y);

            Series.Clear();
            Series.Add(
                RenderUtils.Scatter("Real",interpolateX.AsFloatArray(), interpolateY.AsFloatArray(),color: SKColors.Blue)
            );
            if(_yImagValues is not null)
            {
                var yImag = np.array(_yImagValues,copy:false);
                Series.Add(
                    RenderUtils.Scatter("Imag",interpolateX.AsFloatArray(), yImag.AsFloatArray(),color: SKColors.Orange)
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