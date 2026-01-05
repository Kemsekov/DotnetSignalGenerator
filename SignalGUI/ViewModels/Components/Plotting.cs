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
using Avalonia.Media.Imaging;
using SkiaSharp;
using NWaves.Filters.Bessel;
using System.Numerics;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel
{
    // Properties to hold 2D data
    private ndarray? _2DData;
    private bool _is2DMode = false;

    // Property to hold the rendered 2D image
    private Bitmap? _renderedImage;
    public Bitmap? RenderedImage
    {
        get => _renderedImage;
        private set => SetProperty(ref _renderedImage, value);
    }

    // Chart commands
    [RelayCommand]
    private void PlotLine()
    {
        if (_xValues != null && _yValues != null)
        {
            var x = np.array(_xValues,copy:false).resample([SignalParameters.RenderPoints]);
            var y = np.array(_yValues,copy:false).resample([SignalParameters.RenderPoints]);

            Series.Clear();
            Series.Add(
                RenderUtils.Plot(
                    "Real",
                    x.AsFloatArray(), 
                    y.AsFloatArray(),
                    color: SKColors.Blue
                )
            );
            if(_yImagValues is not null)
            {
                var yImag = np.array(_yImagValues,copy:false).resample([SignalParameters.RenderPoints]);
                Series.Add(
                    RenderUtils.Plot(
                        "Imag",
                        x.AsFloatArray(), 
                        yImag.AsFloatArray(),
                        color: SKColors.Orange
                    )
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
            var x = np.array(_xValues,copy:false).resample([SignalParameters.RenderPoints]);
            var y = np.array(_yValues,copy:false).resample([SignalParameters.RenderPoints]);

            Series.Clear();
            Series.Add(
                RenderUtils.Scatter(
                    "Real",
                    x.AsFloatArray(), 
                    y.AsFloatArray(),
                    color: SKColors.Blue
                )
            );
            if(_yImagValues is not null)
            {
                var yImag = np.array(_yImagValues,copy:false).resample([SignalParameters.RenderPoints]);
                Series.Add(
                    RenderUtils.Scatter(
                        "Imag",
                        x.AsFloatArray(), 
                        yImag.AsFloatArray(),
                        color: SKColors.Orange
                    )
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
    private void Plot2DImage()
    {
        if (_2DData != null)
        {
            SKImage skImage;
            if (_2DData.Dtype == np.Complex)
            {
                skImage = RenderUtils.RenderComplexArrayAsImage(_2DData, 800, 600);
            }
            else
            {
                skImage = RenderUtils.RenderFloatArrayAsImage(_2DData, 800, 600);
            }

            // Convert SKImage to Avalonia Bitmap
            var bitmap = RenderUtils.SKImageToAvaloniaBitmap(skImage);

            // Set the rendered image to the property so it can be displayed in the UI
            RenderedImage = bitmap;

            // Clear the chart series since we're switching to image mode
            Series.Clear();
        }
    }


    [RelayCommand]
    private void ToggleFilterEnabled(FilterItemViewModel filter)
    {
        if (filter != null)
        {
            filter.Enabled = !filter.Enabled;
        }
    }

    [RelayCommand]
    private void Plot2DComplexImage()
    {
        if (_2DData != null && _2DData.Dtype == np.Complex)
        {
            var skImage = RenderUtils.RenderComplexArrayAsImage(_2DData, 800, 600);

            // Convert SKImage to Avalonia Bitmap
            var bitmap = RenderUtils.SKImageToAvaloniaBitmap(skImage);

            // Set the rendered image to the property so it can be displayed in the UI
            RenderedImage = bitmap;

            // Clear the chart series since we're switching to image mode
            Series.Clear();
        }
    }
}