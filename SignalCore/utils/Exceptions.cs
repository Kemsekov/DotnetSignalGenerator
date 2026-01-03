namespace SignalCore;
public static class ExceptionExtensions
{
    public static Exception GetMostInnerException(this Exception e)
    {
        var inner = e;
        while (inner.InnerException is not null)
            inner = inner.InnerException;
        return inner;
    }
}