using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Drawing;
using NumpyDotNet;
using SignalCore;
using System;

public static class RenderUtils
{
    // Scatter Plot: Dots only
    public static ScatterSeries<ObservablePoint> Scatter(string name,IEnumerable<float> X, IEnumerable<float> Y, int pointSize = 10,SKColor? color = null)
    {
        var c = color ?? SKColors.Blue;
        var points = X.Zip(Y, (x, y) => new ObservablePoint(x, y)).ToArray();
        return new ScatterSeries<ObservablePoint> {
            Name=name,
            Values = points,
            GeometrySize = pointSize,
            Stroke = new SolidColorPaint(c),
            XToolTipLabelFormatter = point => $"t={point.Coordinate.SecondaryValue:F3}",
            YToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:F3}",
        };
    }
    // Plot: Line chart
    public static LineSeries<ObservablePoint> Plot(string name, IEnumerable<float> X, IEnumerable<float> Y, int lineThickness = 3,SKColor? color = null)
    {
        var c = color ?? SKColors.Blue;
        var points = X.Zip(Y, (x, y) => new ObservablePoint(x, y)).ToArray();

        return new LineSeries<ObservablePoint> {
            Name=name,
            Values = points,
            Fill = null,          // Remove the area fill under the line
            GeometrySize = 0,     // Remove dots at data points
            Stroke = new SolidColorPaint(c) { StrokeThickness = lineThickness },
            XToolTipLabelFormatter = point => $"t={point.Coordinate.SecondaryValue:F3}",
            YToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:F3}",
        };
    }

    /// <summary>
    /// Renders a 2D float array as an image where values are mapped to grayscale colors
    /// </summary>
    /// <param name="data">2D float array to render</param>
    /// <param name="width">Width of the output image</param>
    /// <param name="height">Height of the output image</param>
    /// <returns>SKImage representing the rendered 2D array</returns>
    public static SKImage RenderFloatArrayAsImage(ndarray data, int width, int height)
    {
        var rows = data.shape[0];
        var cols = data.shape[1];

        // Create a bitmap with the specified dimensions
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        // Calculate cell dimensions
        float cellWidth = (float)width / cols;
        float cellHeight = (float)height / rows;

        // Find min and max values for normalization
        var minVal = np.min(data);
        var maxVal = np.max(data);

        

        var range = (maxVal - minVal).AsFloatArray()[0];
        if (range == 0) range = 1; // Avoid division by zero

        // Draw each cell
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var val = data[i, j];
                var normalized = (val - minVal) / range;

                // Map to grayscale (0-255)
                byte grayValue = (normalized * 255).AsByteArray()[0];
                var color = new SKColor(grayValue, grayValue, grayValue);

                var rect = new SKRect(j * cellWidth, i * cellHeight, (j + 1) * cellWidth, (i + 1) * cellHeight);
                using var paint = new SKPaint { Color = color };
                canvas.DrawRect(rect, paint);
            }
        }

        return SKImage.FromBitmap(bitmap);
    }
    static SKColor ComplexToColor(Complex z, float k = 1.0f)
    {
        // Phase in [-pi, pi], map to hue in [0,360)
        float theta = (float)Math.Atan2(z.Imaginary, z.Real);
        float hue = (theta + MathF.PI) * (180f / MathF.PI); // 0..360

        // Magnitude -> value (brightness) in [0,1] with compression
        float r = (float)z.Magnitude;
        float v = 1f - MathF.Exp(-k * r); // 0..1, smooth, compresses large magnitudes [web:181]

        float s = 1f;                    // full saturation is common in domain coloring [web:179]
        byte alpha = 255;

        // Skia HSV expects H in [0,360), S,V in [0,100]
        return SKColor.FromHsv(hue, s * 100f, v * 100f).WithAlpha(alpha);
    }
    /// <summary>
    /// Renders a 2D Complex array as an image where real and imaginary parts are shown in different colors
    /// </summary>
    /// <param name="data">2D Complex array to render</param>
    /// <param name="width">Width of the output image</param>
    /// <param name="height">Height of the output image</param>
    /// <returns>SKImage representing the rendered 2D Complex array</returns>
    public static SKImage RenderComplexArrayAsImage(ndarray data, int width, int height)
    {
        var rows = data.shape[0];
        var cols = data.shape[1];

        // Create a bitmap with the specified dimensions
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        // Calculate cell dimensions
        float cellWidth = (float)width / cols;
        float cellHeight = (float)height / rows;

        // Find min and max values for normalization
        var minVal = np.min(new[]{np.min(data.Real),np.min(data.Imag)}.ToNdarray());
        var maxVal = np.max(new[]{np.max(data.Real),np.max(data.Imag)}.ToNdarray());
        

        var range = (maxVal - minVal).AsFloatArray()[0];
        if (range == 0) range = 1; // Avoid division by zero

        // Draw each cell
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var val = data[i, j];
                var normalized = (val - minVal) / range;

                // Map to grayscale (0-255)
                // byte realGrayValue = (normalized.Real * 255).AsByteArray()[0];
                // byte ImagGrayValue = (normalized.Imag * 255).AsByteArray()[0];
                var color = ComplexToColor(normalized.AsComplexArray()[0]);
                

                var rect = new SKRect(j * cellWidth, i * cellHeight, (j + 1) * cellWidth, (i + 1) * cellHeight);
                using var paint = new SKPaint { Color = color };
                canvas.DrawRect(rect, paint);
            }
        }

        return SKImage.FromBitmap(bitmap);
    }

    /// <summary>
    /// Converts an SKImage to Avalonia Bitmap
    /// </summary>
    /// <param name="skImage">SKImage to convert</param>
    /// <returns>Avalonia Bitmap</returns>
    public static Avalonia.Media.Imaging.Bitmap SKImageToAvaloniaBitmap(SKImage skImage)
    {
        using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new System.IO.MemoryStream(data.ToArray());
        return new Avalonia.Media.Imaging.Bitmap(stream);
    }
}
