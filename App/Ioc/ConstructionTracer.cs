using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SportDJ.App.Ioc;

/// <summary>
/// Measures how long the construction of registered types takes and writes slow ones to the trace output.
/// "Total" includes everything created inside the constructor (injected dependencies), "self" does not.
/// </summary>
public static class ConstructionTracer
{
    #region Private Fields

    private const double ThresholdInMilliseconds = 10;

    [ThreadStatic]
    private static Stack<long> childTicksStack;

    #endregion Private Fields

    #region Public Methods

    public static object Create(Type type, Func<object> factory)
    {
        childTicksStack ??= new Stack<long>();
        childTicksStack.Push(0);

        var depth = childTicksStack.Count - 1;
        var start = Stopwatch.GetTimestamp();

        try
        {
            return factory.Invoke();
        }
        finally
        {
            var total = Stopwatch.GetElapsedTime(start);
            var childTicks = childTicksStack.Pop();

            if (childTicksStack.Count > 0)
            {
                childTicksStack.Push(childTicksStack.Pop() + total.Ticks);
            }

            var self = total - TimeSpan.FromTicks(childTicks);

            if (total.TotalMilliseconds >= ThresholdInMilliseconds)
            {
                Trace.WriteLine($"[Create] total {total.TotalMilliseconds,7:F0} ms  self {self.TotalMilliseconds,7:F0} ms  {new string(' ', depth * 2)}{type.FullName}");
            }
        }
    }

    #endregion Public Methods
}
