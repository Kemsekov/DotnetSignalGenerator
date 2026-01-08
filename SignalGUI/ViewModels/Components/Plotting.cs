using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using SignalCore;
using NumpyDotNet;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel
{


    // Chart commands
    [RelayCommand]
    private void PlotLine()
    {
        var _xValues = _computeSignal?.X;
        var _yValues = _computeSignal?.Y;
        var _yImagValues = _computeSignal?.YImag;
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
        var _xValues = _computeSignal?.X;
        var _yValues = _computeSignal?.Y;
        var _yImagValues = _computeSignal?.YImag;
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
        var _2DData = _computeSignal?.ImageData;
        if (_2DData != null)
        {
            var renderPoints = SignalParameters.RenderPoints;
            var shape = _2DData.shape.iDims.ToArray();
            shape=shape.Select(v=>v>renderPoints ? renderPoints : v).ToArray();

            _2DData=_2DData.resample(shape);

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
        var _2DData = _computeSignal?.ImageData;
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