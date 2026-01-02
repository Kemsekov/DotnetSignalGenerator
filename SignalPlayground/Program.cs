
using System.ComponentModel.DataAnnotations;
using NumpyDotNet;
using SignalCore;
using SignalCore.Computation;
using SignalCore.Storage;

// to load numpy library
new SinusoidGenerator().Sample(10);

var s1 = new SinusoidGenerator();
var s2 = new TriangleGenerator();
var points = 1024;

var lowpass = Filters.LowPass(0.9f).Compute;
var lowpassSmaller = Filters.LowPass(0.5f).Compute;
var highpass = Filters.HighPass(0.9f).Compute;
var addNoise = Filters.AddNormalNoise(0,0.2f).Compute;
var cutOutlier = new CutOutliersFilter(0.05f,0.95f).Compute;
var normalization = new ZScoreNormalization(0,1).Compute;
var fftT = new FFTTransform().Compute;
var invFftT = new InverseFFTTransform().Compute;
var wt = new FWTTransform().Compute;
var iwt = new InverseFWTTransform().Compute;

var stftT = new STFTTransform(256,256).Compute;
var inverseStftT = new InverseSTFTTransform(256,256).Compute;

var signal = LazyTrackedOperation.Factory(
    ()=>s1.Sample(points),
    ()=>s2.Sample(points)
)
.Transform(signals=>signals[0]+signals[1])
.Transform(data =>
{
    data[1]=addNoise(data.A(1));
    return data;
});

var filterY_ = signal
.Transform(
    data=>data.A(1) //Y
)
.Composition(lowpass,highpass,cutOutlier,lowpassSmaller,normalization);

var transformations = filterY_
.Transform([fftT,stftT,wt]);

var invTransformations = transformations
.Transform([
    t=>invFftT(t[0]),
    t=>inverseStftT(t[1]),
    t=>iwt(t[2]),
]);

invTransformations.OnExecutedStep += i =>
{
    System.Console.WriteLine($"Event {i}\tCompleted {invTransformations.PercentCompleted}");
};

var data = signal.Result;
var X = data.A(0).AsFloatArray();
var origY = data.A(1).AsFloatArray();
var filterY = filterY_.Result;

var transforms = transformations.Result;
var (fft,stft) = (transforms[0],transforms[1]);

var invTransforms = invTransformations.Result;
var (fftInv,stftInv,wtInv) = (invTransforms[0],invTransforms[1],invTransforms[2]);

var plot = LazyTrackedOperation.ActionSequential(
    () => {
        var chart = RenderUtils.Plot(X,origY,lineThickness:1);
        chart.SaveImage("orig.png");
    },
    () => {
        var chart = RenderUtils.Plot(X,filterY.AsFloatArray(),lineThickness:1);
        chart.SaveImage("filter.png");
    },
    () => {
        var chart = RenderUtils.Plot(X,fft.Real.AsFloatArray(),lineThickness:1);
        chart.SaveImage("fft.png");
    },
    () => {
        var chart = RenderUtils.Plot(X,fftInv.Real.AsFloatArray(),lineThickness:1);
        chart.SaveImage("fft_inverse.png");
    },
    () => {
        var chart = RenderUtils.Plot(X,np.max(stft,0).Real.AsFloatArray(),lineThickness:1);
        chart.SaveImage("stft.png");
    },
    ()=>{
        var chart = RenderUtils.Plot(X,stftInv.Real.AsFloatArray(),lineThickness:1);
        chart.SaveImage("stft_inverse.png");
    },
        ()=>{
        var chart = RenderUtils.Plot(X,wtInv.Real.AsFloatArray(),lineThickness:1);
        chart.SaveImage("wt.png");
    }
);

System.Console.WriteLine("Plotting...");
var _ = plot.Result;

System.Console.WriteLine($"Signal Factory {signal.ElapsedMilliseconds} ms");
System.Console.WriteLine($"Filters {filterY_.ElapsedMilliseconds} ms");
System.Console.WriteLine($"Transforms {transformations.ElapsedMilliseconds} ms");
System.Console.WriteLine($"Inv transforms {invTransformations.ElapsedMilliseconds} ms");

