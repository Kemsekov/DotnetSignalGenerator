using DynamicExpresso;
namespace SignalCore;

/// <summary>
/// Apply some expression to multiple signals, such as signal sums, differences, products, etc.
/// In expression you must use same name variables as passed to Call method.
/// In these expressions you can reference np object to get signal exponents, logs, etc.
/// </summary>
public class StringExpression
{
    string _expression;
    Interpreter interpreter;
    public StringExpression(string expression)
    {
        _expression=expression;
        interpreter = new Interpreter()
            .Reference(typeof(np))
            .Reference(typeof(NDArrayExtensions))
            .Reference(typeof(MathF));
    }
    public ndarray Call(params (string name, ndarray signal)[] parameters)
    {
        var _params = parameters.Select(v=>new Parameter(v.name,v.signal)).ToArray();
        var res = (ndarray)interpreter.Eval(_expression,_params);
        return res;
    }
}