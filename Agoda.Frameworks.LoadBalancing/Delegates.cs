using System;
using System.Threading.Tasks;

namespace Agoda.Frameworks.LoadBalancing
{
    public delegate TResult RandomSourceFunc<in TSource, out TResult>(TSource source, int attemptCount);
    public delegate Task<TResult> RandomSourceAsyncFunc<in TSource, TResult>(TSource source, int attemptCount);
    public delegate bool ShouldRetryPredicate(int retryAttempt, Exception exception);
    public delegate void UpdateWeight<in TSource>(TSource source, bool isSuccess);
    public delegate void OnError(Exception error, int attemptCount);
}
