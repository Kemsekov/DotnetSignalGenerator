using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

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
            XToolTipLabelFormatter = point => $"t={point.Coordinate.PrimaryValue:F2}",
            YToolTipLabelFormatter = point => $"y={point.Coordinate.SecondaryValue:F2}",
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
            XToolTipLabelFormatter = point => $"t={point.Coordinate.PrimaryValue:F2}",
            YToolTipLabelFormatter = point => $"y={point.Coordinate.SecondaryValue:F2}",
        };
    }
}
