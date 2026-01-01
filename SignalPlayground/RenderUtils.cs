using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

public static class RenderUtils
{
    
    // Scatter Plot: Dots only
    public static SKCartesianChart Scatter(IEnumerable<float> X, IEnumerable<float> Y, int pointSize = 10)
    {
        var points = X.Zip(Y, (x, y) => new ObservablePoint(x, y)).ToArray();
        return new SKCartesianChart
        {
            Width = 1000,
            Height = 600,
            Series = [
                new ScatterSeries<ObservablePoint> { 
                    Values = points, 
                    GeometrySize = pointSize,
                }
            ]
        };
    }

    // Plot: Line chart
    public static SKCartesianChart Plot(IEnumerable<float> X, IEnumerable<float> Y, int lineThickness = 3)
    {
        var points = X.Zip(Y, (x, y) => new ObservablePoint(x, y)).ToArray();

        return new SKCartesianChart
        {
            Width = 1000,
            Height = 600,
            Series = [
                new LineSeries<ObservablePoint> {
                    Values = points,
                    Fill = null,          // Remove the area fill under the line
                    GeometrySize = 0,     // Remove dots at data points
                    Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = lineThickness } // Set line thickness
                }
            ]
        };
    }
}
