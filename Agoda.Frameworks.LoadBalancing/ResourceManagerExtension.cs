using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agoda.Frameworks.LoadBalancing
{
    public static class ResourceManagerExtension
    {
        public static void UpdateResources<TSource>(
            this IResourceManager<TSource> mgr,
            IEnumerable<TSource> collection)
        {
            mgr.UpdateResources(
                collection
                    .Distinct()
                    .ToDictionary(x => x, _ => WeightItem.CreateDefaultItem()));
        }

        // TODO: Test
        public static TResult ExecuteAction<TSource, TResult>(
            this IResourceManager<TSource> mgr,
            RandomSourceFunc<TSource, TResult> func,
            ShouldRetryPredicate shouldRetry,
            OnError onError = null)
        {
            var retryAction = new RetryAction<TSource>(mgr.SelectRandomly, mgr.UpdateWeight);
            return retryAction.ExecuteAction(func, shouldRetry, onError);
        }

        public static Task<TResult> ExecuteAsync<TSource, TResult>(
            this IResourceManager<TSource> mgr,
            RandomSourceAsyncFunc<TSource, TResult> taskFunc,
            ShouldRetryPredicate shouldRetry,
            OnError onError = null)
        {
            var retryAction = new RetryAction<TSource>(mgr.SelectRandomly, mgr.UpdateWeight);
            return retryAction.ExecuteAsync(taskFunc, shouldRetry, onError);
        }

        public static Task<IReadOnlyList<RetryActionResult<TSource, TResult>>> ExecuteAsyncWithDiag<TSource, TResult>(
            this IResourceManager<TSource> mgr,
            RandomSourceAsyncFunc<TSource, TResult> taskFunc,
            ShouldRetryPredicate shouldRetry,
            OnError onError = null)
        {
            var retryAction = new RetryAction<TSource>(mgr.SelectRandomly, mgr.UpdateWeight);
            return retryAction.ExecuteAsyncWithDiag(taskFunc, shouldRetry, onError);
        }
    }
}