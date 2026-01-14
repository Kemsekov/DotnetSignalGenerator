using SignalCore.Computation;

namespace SignalCore;

/// <summary>
/// Computed signal instance, this class contains only data data fields
/// </summary>
public class ComputedSignal : ICloneable
{
    // Properties to hold 1d signal data
    public float[]? X{get;set;}
    public float[]? Y{get;set;}
    public float[]? YImag{get;set;}
    // Properties to hold 2D data (like Wavelet(FWT) transform)
    public ndarray? ImageData{get;set;}
    // Signal statistics like mean, std, skew, amplitude, etc
    public (float stat, string name)[]? Stats{get;set;}
    public ComputedSignal(){}
    public ComputedSignal(ndarray X, ndarray Y, (float stat, string name)[] stats)
    {
        this.X = X?.AsFloatArray();
        Stats = stats;
        // if we got single signal output of shape [1,N] or [N]
        if (Y.shape[0] == 1 && Y.shape.iDims.Length == 2 || Y.shape.iDims.Length == 1)
        {
            // if we have complex outputs like FFT, keep separate real
            // and imag part of signal
            if (Y.Dtype == np.Complex)
                YImag = Y.Imag?.AsFloatArray();
            else
                YImag = null;

            // here X is signal time, Y is signal value

            // Store the X and Y values for plotting
            this.Y = Y.Real?.AsFloatArray();
        }

        // if we have 2d array, then render it as image
        if(Y.shape[0]>1 && Y.shape.iDims.Length == 2)
        {
            ImageData = Y;
        }

        System.Console.WriteLine("Shapes");
        System.Console.WriteLine(this.X?.Length);
        System.Console.WriteLine(this.Y?.Length);
        System.Console.WriteLine(this.ImageData?.shape);
    }
    public ComputedSignal Clone()
    {
        //Shallow copy
        return new()
        {
            ImageData=ImageData,
            Stats=Stats,
            X=X,
            Y=Y,
            YImag=YImag
        };
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}

/// <summary>
/// Class that implements parallel 
/// non-blocking simplified signal's computation logic
/// </summary>
public class ComputeSignal : ICloneable
{
    TrackedOperation<ndarray> combineSources;
    TrackedOperation<ndarray> createdSignal;
    TrackedOperation<(float stat, string name)[]>? signalStatistics = null;

    public ComputeSignal(
        int computePoints,
        IEnumerable<(string letter, ISignalGenerator instance)> generators,
        string expression,
        IEnumerable<ISignalOperation> ops,
        IEnumerable<ISignalStatistic> statistics)
    {
        ComputePoints = computePoints;
        Generators = generators;
        Expression = expression;
        Ops = ops;
        Statistics = statistics;
        
        var expr = new StringExpression(expression);
        // Method to create function that sample signal 
        // from given generator and assign a name to it
        Func<(string name, ndarray signal)> SignalFactory(ISignalGenerator g, string signalLetter)
            => () => (
                signalLetter,
                g.Sample(computePoints)
            );

        //this one creates signal sources
        var generationOperation = LazyTrackedOperation.Factory(
            generators.Select(v => SignalFactory(v.instance, v.letter)).ToArray()
        );

        // this one combines multiple sources into single signal
        combineSources =
            generationOperation
            .Transform(v =>
                // drop X values before applying expression
                v.Select(pair => (pair.name, pair.signal.at(1)))
                .ToArray()
            )
            .Transform(expr.Call)
            .Transform(s =>
            {
                //Add again X value after expression is applied
                var X = generationOperation.Result[0].signal.at(0);
                return np.concatenate([X, s]);
            });

        //this one applies filters/transformations/normalizations/etc
        createdSignal = combineSources.Composition(
            [s=>s.at(1), // first select Y dimension
            // then apply filters,transforms, normalizations,etc
            ..
            ops.Select(v=>(Func<ndarray,ndarray>)v.Compute)]
        );

        // this one computes all signal statistics
        (float stat,string name) ComputeStatistic(ndarray signal, ISignalStatistic stat)
        {
            var value = np.round(stat.Compute(signal),3).AsFloatArray()[0];
            return (value,stat.Name);
        }

        //Compute all signal stats at parallel
        signalStatistics = createdSignal.Transform(
            statistics.Select(
                stat=>(Func<ndarray,(float stat,string name)>)(
                    signal=>ComputeStatistic(signal,stat)
                )
            ).ToArray()
        );

        signalStatistics.OnExecutionDone += statsRaw =>
        {
            // Need to dispatch to UI thread since this event is called from background thread

            var res = createdSignal.Result;
            var genOut = combineSources.Result;
            System.Console.WriteLine("====================");
            System.Console.WriteLine($"Time {genOut?.shape}");
            System.Console.WriteLine($"Signal {res.shape}");
            System.Console.WriteLine($"Signal dtype {res.Dtype}");
            this.ComputedSignal = new ComputedSignal(genOut?.at(0) ?? throw new Exception(),res,statsRaw);

            OnExecutionDone.Invoke();
        };
        signalStatistics.OnExecutedStep+=i=>OnExecutedStep(i);
        signalStatistics.CancelState.OnCancel+= ()=>OnCancel();
        signalStatistics.OnException+= e=> OnException(e);
    }
    public float PercentCompleted => signalStatistics?.PercentCompleted ?? 0;
    public int ComputePoints { get; } = 1024;
    public string Expression { get; } = "";
    public IEnumerable<(string letter, ISignalGenerator instance)> Generators { get; } = [];
    public IEnumerable<ISignalOperation> Ops { get; } = [];
    public IEnumerable<ISignalStatistic> Statistics { get; } = [];
    public ComputedSignal ComputedSignal { get; private set; } = new();

    public Action OnCancel = ()=>{};
    public event Action<Exception> OnException = e=>{};
    public event Action<int> OnExecutedStep = i=>{};
    public event Action OnExecutionDone = ()=>{};
    //Run the latest chain element
    public void Run() => signalStatistics?.Run();
    // Cancel computation
    public void Cancel()=>signalStatistics?.Cancel();
    /// <summary>
    /// Waits for task completion
    /// </summary>
    public void Wait()=>signalStatistics?.Await().Wait();
    public ComputeSignal Clone()
    {
        return new ComputeSignal(
            ComputePoints,
            Generators,
            Expression,
            Ops,
            Statistics
        )
        {
            ComputedSignal=this.ComputedSignal.Clone()         
        };
    }
    object ICloneable.Clone()
    {
        return Clone();
    }
}