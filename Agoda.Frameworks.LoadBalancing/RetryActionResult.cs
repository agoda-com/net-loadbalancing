using System;

namespace Agoda.Frameworks.LoadBalancing
{
    public sealed class RetryActionResult<TSource, TResult>
    {
        public RetryActionResult(
            TSource source,
            TResult result,
            TimeSpan elapsed,
            Exception exception,
            int attempt)
        {
            Source = source;
            Result = result;
            Elapsed = elapsed;
            Exception = exception;
            Attempt = attempt;
        }

        public TSource Source { get; }
        public TResult Result { get; }
        public TimeSpan Elapsed { get; }
        public Exception Exception { get; }
        public int Attempt { get; }
        public bool IsError => Exception != null;
    }
}
