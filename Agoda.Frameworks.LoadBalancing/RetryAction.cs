using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Agoda.Frameworks.LoadBalancing
{
    public class RetryAction<TSource>
    {
        private readonly UpdateWeight<TSource> _updateWeight;
        private readonly Func<TSource> _chooseRandomly;

        public RetryAction(
            Func<TSource> chooseRandomly,
            UpdateWeight<TSource> updateWeight)
        {
            _chooseRandomly = chooseRandomly;
            _updateWeight = updateWeight;
        }

        public TResult ExecuteAction<TResult>(
            RandomSourceFunc<TSource, TResult> func,
            ShouldRetryPredicate shouldRetry,
            OnError onError = null)
        {
            if (typeof(Task).IsAssignableFrom(typeof(TResult)))
            {
                throw new ArgumentException(
                    "Async action should be executed with ExecuteAsync.",
                    nameof(func));
            }
            for (var attemptCount = 1; ; attemptCount++)
            {
                var item = _chooseRandomly();
                try
                {
                    var result = func(item, attemptCount);

                    _updateWeight(item, true);

                    return result;
                }
                catch (Exception e)
                {
                    onError?.Invoke(e, attemptCount);
                    _updateWeight(item, false);
                    if (!shouldRetry(attemptCount, e))
                    {
                        throw;
                    }
                }
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(
            RandomSourceAsyncFunc<TSource, TResult> taskFunc,
            ShouldRetryPredicate shouldRetry,
            OnError onError = null)
        {
            for (var attemptCount = 1; ; attemptCount++)
            {
                var item = _chooseRandomly();
                try
                {
                    var result = await taskFunc(item, attemptCount);

                    _updateWeight(item, true);

                    return result;
                }
                catch (Exception e)
                {
                    onError?.Invoke(e, attemptCount);
                    _updateWeight(item, false);
                    if (!shouldRetry(attemptCount, e))
                    {
                        throw;
                    }
                }
            }
        }

        public async Task<IReadOnlyList<RetryActionResult<TSource, TResult>>> ExecuteAsyncWithDiag<TResult>(
            RandomSourceAsyncFunc<TSource, TResult> taskFunc,
            ShouldRetryPredicate shouldRetry,
            OnError onError = null)
        {
            var stopwatch = new Stopwatch();
            var results = new List<RetryActionResult<TSource, TResult>>();
            for (var attemptCount = 1; ; attemptCount++)
            {
                var item = _chooseRandomly();
                try
                {
                    stopwatch.Restart();
                    var result = await taskFunc(item, attemptCount);
                    _updateWeight(item, true);
                    results.Add(new RetryActionResult<TSource, TResult>(
                        item, result, stopwatch.Elapsed, null, attemptCount));
                    break;
                }
                catch (Exception e)
                {
                    results.Add(new RetryActionResult<TSource, TResult>(
                        item, default(TResult), stopwatch.Elapsed, e, attemptCount));
                    _updateWeight(item, false);
                    var isFailed = !shouldRetry(attemptCount, e);
                    if (isFailed)
                    {
                        break;
                    }
                }
            }
            return results;
        }
    }
}
