using System;
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
    }
}
