//TODO: Add tests
// 1. Check if computation works (all methods works as intended)
// 2. Check if cancellation system works, so if we have multiple
// tracked operations like T1->T2->T3, then stopping next one will stop all previous
// 3. Check that all events like OnExecutedStep, OnCancel, OnException is called.
// when operation throws exception
// 3.1 Check that if exception on previous tracked events happened, it properly propagates to forward tracked ops as well,
// so if we have chain T1->T2->T3, and T1 throws, then T1, T2 and T3 OnException is called
// 4. Check that ElapsedMilliseconds time for operation is properly computed
// for different cases: when we have chain T1->T2->T3, and some previous steps are
// precomputed, when not precomputed. Check that time works as intended
// 5. Check that ExecutedSteps,TotalSteps are updated properly with chained
// lazy operations and accumulates.
using System.Diagnostics;
namespace SignalCore.Computation;

public class LazyOperationState
{
    public event Action OnCancel = ()=>{};
    public bool Canceled => _canceled;
    bool _canceled = false;
    public void Cancel()
    {
        if(_canceled) return;
        _canceled = true;
        OnCancel();
    }
    public void ThrowIfCanceled()
    {
        if (_canceled)
            throw new OperationCanceledException("Tracked lazy operation was canceled");
    }
}

/// <summary>
/// Represents a computation that can be executed asynchronously with progress tracking.
/// </summary>
/// <typeparam name="T">The type of the result produced by the operation.</typeparam>
public class TrackedOperation<T>(Func<Action<int>,LazyOperationState, T> jobUnit, int totalSteps, Func<int>? previousOperationExecutedSteps = null, int previousOperationTotalSteps = 0)
{
    /// <summary>
    /// Event fired when a step in the operation is executed.
    /// </summary>
    public event Action<int> OnExecutedStep = a => { };
    public event Action<T> OnExecutionDone = (i) => {};
    public event Action<Exception> OnException = (i) => {};
    /// <summary>
    /// Exceptions (if any) of running task
    /// </summary>
    public AggregateException? Exception => _running?.Exception;
    Stopwatch watch = new();
    Task<T>? _running = null;
    int[] executedSteps = new int[totalSteps];
    
    /// <summary>
    /// Gets the number of steps that have been executed, including previous operations.
    /// </summary>
    public int ExecutedSteps => executedSteps.Sum() + (previousOperationExecutedSteps?.Invoke() ?? 0);
    
    /// <summary>
    /// Gets the total number of steps in the operation, including previous operations.
    /// </summary>
    public int TotalSteps => totalSteps + previousOperationTotalSteps;
    
    /// <summary>
    /// Gets the percentage of the operation that has been completed (from 0.0 to 1.0).
    /// </summary>
    public float PercentCompleted => (float)ExecutedSteps / TotalSteps;
    
    /// <summary>
    /// Gets the elapsed time in milliseconds since the operation started. <br/>
    /// Note: if previous tracked operation is chained with current one, then <br/>
    /// 1. If previous tracked operation have not been computed, the ElapsedMilliseconds will contain total time for it's own execution
    /// and execution time of chained tracked operation<br/>
    /// 2. If previous tracked operation have been computed beforehand, the ElapsedMilliseconds will contain just time
    /// of this instance object computation.
    /// </summary>
    public long ElapsedMilliseconds => watch.ElapsedMilliseconds;
    
    /// <summary>
    /// Gets whether the operation is currently running.
    /// </summary>
    public bool IsRunning => watch.IsRunning;
    public LazyOperationState CancelState {get;} = new();
    public void Cancel()=>CancelState.Cancel();
    /// <summary>
    /// Starts the execution of the operation if it hasn't already been started.
    /// </summary>
    public void Run()
    {
        if (_running is not null) return;
        watch.Start();
        _running = Task.Run(() =>
        {
            try{
                var res = jobUnit(i =>
                {
                    executedSteps[i] = 1;
                    OnExecutedStep(previousOperationTotalSteps + i);
                },CancelState);
                watch.Stop();
                OnExecutionDone(res);
                return res;
            }
            catch(Exception e)
            {
                watch.Stop();
                OnException(e);
                throw;
            }
        });
    }
    
    /// <summary>
    /// Gets the result of the operation, blocking until completion if necessary.
    /// </summary>
    public T Result => Await().Result;
    
    /// <summary>
    /// Asynchronously waits for the operation to complete and returns the result.
    /// </summary>
    /// <returns>A task that completes with the result of the operation.</returns>
    public async Task<T> Await()
    {
        if (_running is null)
        {
            Run();
            return await Await();
        }
        return _running.Result;
    }
    
    /// <summary>
    /// Invokes the execution event for a specific step index.
    /// </summary>
    /// <param name="i">The step index to invoke the event for.</param>
    public void InvokeExecutionEvent(int i)
    {
        OnExecutedStep.Invoke(i);
    }
}

/// <summary>
/// Provides utility methods for creating and composing tracked operations that can be executed lazily with progress tracking.
/// </summary>
public static class LazyTrackedOperation
{
    /// <summary>
    /// Sequentially applies a list of transformations to an input value in a composition-like flow.
    /// </summary>
    /// <typeparam name="T">The type of the input and output values.</typeparam>
    /// <param name="input">The initial input value to transform.</param>
    /// <param name="transforms">The sequence of transformations to apply.</param>
    /// <returns>A TrackedOperation that will execute the transformations sequentially.</returns>
    public static TrackedOperation<T> Composition<T>(T input, params Func<T, T>[] transforms)
    {
        var totalOperations = transforms.Length;
        T delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            var data = input;
            for (int i = 0; i < transforms.Length; i++)
            {
                op.ThrowIfCanceled();
                data = transforms[i](data);
                onTaskCompleted(i);
            }
            return data;
        }
        return new TrackedOperation<T>(delayedTask, totalOperations);
    }
    
    /// <summary>
    /// Sequentially applies a list of transformations to the result of a previous TrackedOperation in a composition-like flow.
    /// </summary>
    /// <typeparam name="T">The type of the input and output values.</typeparam>
    /// <param name="input">The TrackedOperation whose result will be transformed.</param>
    /// <param name="transforms">The sequence of transformations to apply.</param>
    /// <returns>A TrackedOperation that will execute the transformations sequentially on the input's result.</returns>
    public static TrackedOperation<T> Composition<T>(this TrackedOperation<T> input, params Func<T, T>[] transforms)
    {
        var totalOperations = transforms.Length;
        T delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            //this one can compute very long, so we check cancel task before
            // getting previous track
            var data = input.Result;
            for (int i = 0; i < transforms.Length; i++)
            {
                op.ThrowIfCanceled();
                data = transforms[i](data);
                onTaskCompleted(i);
            }
            return data;
        }
        var res = new TrackedOperation<T>(delayedTask, totalOperations, () => input.ExecutedSteps, input.TotalSteps);
        input.OnExecutedStep += res.InvokeExecutionEvent;
        
        //if we cancel next, so we stop previous
        res.CancelState.OnCancel+=input.Cancel;
        return res;
    }
    
    /// <summary>
    /// Creates a TrackedOperation that executes multiple factory functions in parallel and returns an array of their results.
    /// </summary>
    /// <typeparam name="T">The type of the values produced by the factory functions.</typeparam>
    /// <param name="get">The factory functions to execute in parallel.</param>
    /// <returns>A TrackedOperation that will execute the factory functions in parallel.</returns>
    public static TrackedOperation<T[]> Factory<T>(params Func<T>[] get)
    {
        var totalOperations = get.Length;
        T[] delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var res = new T[get.Length];
            var data = get.Select((factory, ind) => (factory, ind));
            Parallel.ForEach(data, v =>
            {
                if(op.Canceled)
                    return;
                res[v.ind] = v.factory();
                onTaskCompleted(v.ind);
            });
            op.ThrowIfCanceled();
            return res;
        }
        return new TrackedOperation<T[]>(delayedTask, totalOperations);
    }
    /// <summary>
    /// Creates a TrackedOperation that executes multiple factory functions in sequential and returns an array of their results.
    /// </summary>
    /// <typeparam name="T">The type of the values produced by the factory functions.</typeparam>
    /// <param name="get">The factory functions to execute.</param>
    /// <returns>A TrackedOperation that will execute the factory functions one by one.</returns>
    public static TrackedOperation<T[]> FactorySequential<T>(params Func<T>[] get)
    {
        var totalOperations = get.Length;
        T[] delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var res = new T[get.Length];
            var data = get.Select((factory, ind) => (factory, ind));
            foreach(var (factory, ind) in data)
            {
                op.ThrowIfCanceled();
                res[ind] = factory();
                onTaskCompleted(ind);
            };
            return res;
        }
        return new TrackedOperation<T[]>(delayedTask, totalOperations);
    }
    
    /// <summary>
    /// Creates a TrackedOperation that executes a sequence of actions sequentially, tracking progress for each completed action.
    /// </summary>
    /// <param name="todo">The actions to execute sequentially.</param>
    /// <returns>A TrackedOperation that will execute the actions sequentially.</returns>
    public static TrackedOperation<bool> ActionSequential(params Action[] todo)
    {
        var totalOperations = todo.Length;
        bool delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            var data = todo.Select((action, ind) => (action, ind));
            foreach (var v in data)
            {
                op.ThrowIfCanceled();
                v.action();
                onTaskCompleted(v.ind);
            }
            return true;
        }
        return new TrackedOperation<bool>(delayedTask, totalOperations);
    }
    
    /// <summary>
    /// Creates a TrackedOperation that executes all actions in parallel, tracking progress for each completed action.
    /// </summary>
    /// <param name="todo">The actions to execute in parallel.</param>
    /// <returns>A TrackedOperation that will execute the actions in parallel.</returns>
    public static TrackedOperation<bool> Action(params Action[] todo)
    {
        var totalOperations = todo.Length;
        bool delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var data = todo.Select((action, ind) => (action, ind));
            Parallel.ForEach(data, v =>
            {
                if(op.Canceled) return;
                v.action();
                onTaskCompleted(v.ind);
            });
            op.ThrowIfCanceled();
            return true;
        }
        return new TrackedOperation<bool>(delayedTask, totalOperations);
    }
    
    /// <summary>
    /// Creates a TrackedOperation that executes all actions in parallel on a given argument, tracking progress for each completed action.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument to pass to each action.</typeparam>
    /// <param name="arg">The argument to pass to each action.</param>
    /// <param name="todo">The actions to execute in parallel with the argument.</param>
    /// <returns>A TrackedOperation that will execute the actions in parallel.</returns>
    public static TrackedOperation<bool> Action<TArg>(TArg arg, params Action<TArg>[] todo)
    {
        var totalOperations = todo.Length;
        bool delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var data = todo.Select((action, ind) => (action, ind));
            Parallel.ForEach(data, v =>
            {
                if(op.Canceled) return;
                v.action(arg);
                onTaskCompleted(v.ind);
            });
            op.ThrowIfCanceled();
            return true;
        }
        return new TrackedOperation<bool>(delayedTask, totalOperations);
    }
    
    /// <summary>
    /// Creates a TrackedOperation that executes all actions in parallel on the result of a TrackedOperation, tracking progress for each completed action.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument to pass to each action.</typeparam>
    /// <param name="arg">The TrackedOperation whose result will be passed to each action.</param>
    /// <param name="todo">The actions to execute in parallel with the TrackedOperation's result.</param>
    /// <returns>A TrackedOperation that will execute the actions in parallel.</returns>
    public static TrackedOperation<bool> Action<TArg>(this TrackedOperation<TArg> arg, params Action<TArg>[] todo)
    {
        var totalOperations = todo.Length;
        bool delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var data = todo.Select((action, ind) => (action, ind));
            var argResult = arg.Result;
            op.ThrowIfCanceled();
            Parallel.ForEach(data, v =>
            {
                if(op.Canceled) return;
                v.action(argResult);
                onTaskCompleted(v.ind);
            });
            op.ThrowIfCanceled();
            return true;
        }
        var res = new TrackedOperation<bool>(delayedTask, totalOperations);
        arg.OnExecutedStep += res.InvokeExecutionEvent;
        //if we cancel next, so we stop previous
        res.CancelState.OnCancel+=arg.Cancel;
        return res;
    }

    /// <summary>
    /// Applies multiple transformations to an argument in parallel, returning an array of the results.
    /// </summary>
    /// <typeparam name="Arg">The type of the input argument.</typeparam>
    /// <typeparam name="T">The type of the output values.</typeparam>
    /// <param name="arg">The argument to transform.</param>
    /// <param name="transform">The transformation functions to apply in parallel.</param>
    /// <returns>A TrackedOperation that will apply the transformations in parallel.</returns>
    public static TrackedOperation<T[]> Transform<Arg, T>(Arg arg, Func<Arg, T>[] transform)
    {
        var totalOperations = transform.Length;
        T[] delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var res = new T[transform.Length];
            var data = transform.Select((factory, ind) => (factory, ind));
            Parallel.ForEach(data, v =>
            {
                if(op.Canceled) return;
                res[v.ind] = v.factory(arg);
                onTaskCompleted(v.ind);
            });
            op.ThrowIfCanceled();
            return res;
        }
        return new TrackedOperation<T[]>(delayedTask, totalOperations);
    }
    
    /// <summary>
    /// Applies a single transformation to the result of a TrackedOperation.
    /// </summary>
    /// <typeparam name="Arg">The type of the input value.</typeparam>
    /// <typeparam name="T">The type of the output value.</typeparam>
    /// <param name="arg">The TrackedOperation whose result will be transformed.</param>
    /// <param name="transform">The transformation function to apply.</param>
    /// <returns>A TrackedOperation that will apply the transformation to the input's result.</returns>
    public static TrackedOperation<T> Transform<Arg, T>(this TrackedOperation<Arg> arg, Func<Arg, T> transform)
    {
        var totalOperations = 1;
        T delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var prev = arg.Result; //because this one can be very long, it make
            op.ThrowIfCanceled();  //sense to check twice if operation is canceled

            var res = transform(prev);
            onTaskCompleted(0);
            return res;
        }
        var res = new TrackedOperation<T>(delayedTask, totalOperations, () => arg.ExecutedSteps, arg.TotalSteps);
        arg.OnExecutedStep += res.InvokeExecutionEvent;
        //if we cancel next, so we stop previous
        res.CancelState.OnCancel+=arg.Cancel;
        return res;
    }

    /// <summary>
    /// Applies multiple transformations to the result of a TrackedOperation in parallel, returning an array of the results.
    /// </summary>
    /// <typeparam name="Arg">The type of the input value.</typeparam>
    /// <typeparam name="T">The type of the output values.</typeparam>
    /// <param name="arg">The TrackedOperation whose result will be transformed.</param>
    /// <param name="transform">The transformation functions to apply in parallel.</param>
    /// <returns>A TrackedOperation that will apply the transformations in parallel to the input's result.</returns>
    public static TrackedOperation<T[]> Transform<Arg, T>(this TrackedOperation<Arg> arg, Func<Arg, T>[] transform)
    {
        var totalOperations = transform.Length;
        T[] delayedTask(Action<int> onTaskCompleted,LazyOperationState op)
        {
            op.ThrowIfCanceled();
            var res = new T[transform.Length];
            var data = transform.Select((factory, ind) => (factory, ind));
            var input = arg.Result;
            op.ThrowIfCanceled();
            Parallel.ForEach(data, v =>
            {
                if(op.Canceled) return;
                res[v.ind] = v.factory(input);
                onTaskCompleted(v.ind);
            });
            op.ThrowIfCanceled();
            return res;
        }
        var res = new TrackedOperation<T[]>(delayedTask, totalOperations, () => arg.ExecutedSteps, arg.TotalSteps);
        arg.OnExecutedStep += res.InvokeExecutionEvent;
        //if we cancel next, so we stop previous
        res.CancelState.OnCancel+=arg.Cancel;
        return res;
    }
}