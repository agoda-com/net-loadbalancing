using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Agoda.LoadBalancing
{
    public interface IRetryManager<out TSource>
    {
        IEnumerable<WeightItem> Collection { get; }
        TResult ExecuteAction<TResult>(Func<TSource, int, TResult> func);
        Task<TResult> ExecuteAsync<TResult>(Func<TSource, int, Task<TResult>> taskFunc);
        // TODO: Support CancellationToken?
    }

    public class DynamicWeightRetryManager<TSource> : IRetryManager<TSource>
    {
        private ImmutableDictionary<TSource, WeightItem> _collection;

        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
        private readonly Func<int, Exception, bool> _shouldRetry;
        private readonly IWeightManipulationStrategy _weightManipulationStrategy;

        // Hiding source objects is intended.
        public IEnumerable<WeightItem> Collection => _collection.Values;

        public event EventHandler<UpdateWeightEventArgs> OnUpdateWeight;
        public event EventHandler<UpdateWeightEventArgs> OnAllSourcesReachBottom;

        public DynamicWeightRetryManager(
            IReadOnlyDictionary<TSource, WeightItem> collection,
            IWeightManipulationStrategy weightManipulationStrategy,
            Func<int, Exception, bool> shouldRetry)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Source collection must not be null.", nameof(collection));
            }
            if (collection.Count == 0)
            {
                throw new ArgumentException("Source collection must not be empty.", nameof(collection));
            }
            _collection = collection.ToImmutableDictionary();
            _weightManipulationStrategy = weightManipulationStrategy;
            _shouldRetry = shouldRetry;
        }

        public TResult ExecuteAction<TResult>(Func<TSource, int, TResult> func)
        {
            TResult result;
            KeyValuePair<TSource, WeightItem> item;
            for (var retryCount = 0; ;retryCount++)
            {
                item = SelectRandomly();
                try
                {
                    result = func(item.Key, retryCount);
                    break;
                }
                catch (Exception e) when (_shouldRetry(retryCount + 1, e))
                {
                    UpdateWeight(item, false);
                }
                catch
                {
                    UpdateWeight(item, false);
                    throw;
                }
            }
            UpdateWeight(item, true);
            return result;
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<TSource, int, Task<TResult>> taskFunc)
        {
            TResult result;
            KeyValuePair<TSource, WeightItem> item;
            for (var retryCount = 0; ; retryCount++)
            {
                item = SelectRandomly();
                try
                {
                    result = await taskFunc(item.Key, retryCount);
                    break;
                }
                catch (Exception e) when (_shouldRetry(retryCount + 1, e))
                {
                    UpdateWeight(item, false);
                }
                catch
                {
                    UpdateWeight(item, false);
                    throw;
                }
            }
            UpdateWeight(item, true);
            return result;
        }

        private KeyValuePair<TSource, WeightItem> SelectRandomly()
        {
            var sum = _collection.Values.Sum(x => x.Weight);
            var rand = _random.Value.Next(0, sum);
            foreach (var pair in _collection)
            {
                if (rand < pair.Value.Weight)
                {
                    return pair;
                }

                rand = rand - pair.Value.Weight;
            }

            throw new InvalidOperationException("Invalid weight in the collection.");
        }

        private void UpdateWeight(KeyValuePair<TSource, WeightItem> item, bool isSuccess)
        {
            lock (_collection)
            {
                _collection = _weightManipulationStrategy.UpdateWeight(
                    _collection,
                    item.Key,
                    item.Value,
                    isSuccess);
            }
        }

        protected virtual void OnOnUpdateWeight() =>
            OnUpdateWeight?.Invoke(this, new UpdateWeightEventArgs(Collection));

        protected virtual void OnOnAllSourcesReachBottom() =>
            OnAllSourcesReachBottom?.Invoke(this, new UpdateWeightEventArgs(Collection));
    }
}
