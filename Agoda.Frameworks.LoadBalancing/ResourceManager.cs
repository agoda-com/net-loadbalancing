using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentlySynchronizedField
namespace Agoda.Frameworks.LoadBalancing
{
    public interface IResourceManager<TSource>
    {
        IReadOnlyDictionary<TSource, WeightItem> Resources { get; }

        TSource SelectRandomly();
        void UpdateWeight(TSource source, bool isSuccess);
        void UpdateResources(IReadOnlyDictionary<TSource, WeightItem> newResources);

        event EventHandler<UpdateWeightEventArgs> OnUpdateWeight;
        event EventHandler<UpdateWeightEventArgs> OnAllSourcesReachBottom;
    }

    public class ResourceManager<TSource> : IResourceManager<TSource>
    {
        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
        private ImmutableDictionary<TSource, WeightItem> _collection;
        private readonly IWeightManipulationStrategy _weightManipulationStrategy;

        public IReadOnlyDictionary<TSource, WeightItem> Resources => _collection;

        public event EventHandler<UpdateWeightEventArgs> OnUpdateWeight;
        public event EventHandler<UpdateWeightEventArgs> OnAllSourcesReachBottom;

        private static void CheckCollectionArg(IReadOnlyDictionary<TSource, WeightItem> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Source collection must not be null.", nameof(collection));
            }

            if (collection.Count == 0)
            {
                throw new ArgumentException("Source collection must not be empty.", nameof(collection));
            }
        }

        public ResourceManager(
            IReadOnlyDictionary<TSource, WeightItem> collection,
            IWeightManipulationStrategy weightManipulationStrategy)
        {
            CheckCollectionArg(collection);

            _collection = collection.ToImmutableDictionary();
            _weightManipulationStrategy = weightManipulationStrategy;
        }

        public TSource SelectRandomly()
        {
            var collection = _collection;
            var sum = collection.Values.Sum(x => x.Weight);
            // TODO: Or Math.Floor(randomDouble * sum)?
            var rand = _random.Value.Next(0, sum);
            foreach (var pair in collection)
            {
                if (rand < pair.Value.Weight)
                {
                    return pair.Key;
                }

                rand = rand - pair.Value.Weight;
            }

            throw new InvalidOperationException("Invalid weight in the collection.");
        }

        public void UpdateWeight(TSource source, bool isSuccess)
        {
            ImmutableDictionary<TSource, WeightItem> oldCollection;
            ImmutableDictionary<TSource, WeightItem> newCollection;
            lock (_collection)
            {
                oldCollection = _collection;
                if (oldCollection.TryGetValue(source, out var weight))
                {
                    newCollection = oldCollection.SetItem(
                        source,
                        _weightManipulationStrategy.UpdateWeight(
                            source,
                            weight,
                            isSuccess));
                    _collection = newCollection;
                }
                else
                {
                    newCollection = oldCollection;
                }
            }

            RaiseWeightUpdateEvent(oldCollection, newCollection);
        }

        public void UpdateResources(IReadOnlyDictionary<TSource, WeightItem> collection)
        {
            CheckCollectionArg(collection);

            ImmutableDictionary<TSource, WeightItem> oldCollection;
            ImmutableDictionary<TSource, WeightItem> newCollection;
            lock (_collection)
            {
                oldCollection = _collection;
                newCollection = collection
                    .ToImmutableDictionary(
                        x => x.Key,
                        x => oldCollection.TryGetValue(x.Key, out var weight)
                            ? weight
                            : x.Value);
                _collection = newCollection;
            }
            RaiseWeightUpdateEvent(oldCollection, newCollection);
        }

        private void RaiseWeightUpdateEvent(
            ImmutableDictionary<TSource, WeightItem> oldCollection,
            ImmutableDictionary<TSource, WeightItem> newCollection)
        {
            if (oldCollection != newCollection)
            {
                RaiseOnUpdateWeight(newCollection.Values);
                if (newCollection.Values.All(x => x.Weight == x.MinWeight))
                {
                    RaiseOnAllSourcesReachBottom(newCollection.Values);
                }
            }
        }

        protected virtual void RaiseOnUpdateWeight(IEnumerable<WeightItem> weights) =>
            OnUpdateWeight?.Invoke(this, new UpdateWeightEventArgs(weights));

        protected virtual void RaiseOnAllSourcesReachBottom(IEnumerable<WeightItem> weights) =>
            OnAllSourcesReachBottom?.Invoke(this, new UpdateWeightEventArgs(weights));
    }

    public static class ResourceManagerExtension
    {
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
