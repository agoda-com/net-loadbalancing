using System;
using System.Collections.Concurrent;
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

        event EventHandler<UpdateWeightEventArgs<TSource>> OnUpdateWeight;
        event EventHandler<UpdateWeightEventArgs<TSource>> OnAllSourcesReachBottom;
    }

    public static class ResourceManager
    {
        public static IResourceManager<TSource> Create<TSource>(IEnumerable<TSource> sources)
        {
            var resources = new ConcurrentDictionary<TSource, WeightItem>();

            foreach (var source in sources)
            {
                resources.GetOrAdd(source,  WeightItem.CreateDefaultItem());
            }
            return new ResourceManager<TSource>(resources, new AgodaWeightManipulationStrategy());
        }
    }

    public class ResourceManager<TSource> : IResourceManager<TSource>
    {
        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
        private ImmutableDictionary<TSource, WeightItem> _collection;
        private readonly IWeightManipulationStrategy _weightManipulationStrategy;

        public IReadOnlyDictionary<TSource, WeightItem> Resources => _collection;

        public event EventHandler<UpdateWeightEventArgs<TSource>> OnUpdateWeight;
        public event EventHandler<UpdateWeightEventArgs<TSource>> OnAllSourcesReachBottom;

        private static void CheckCollectionArg(IReadOnlyDictionary<TSource, WeightItem> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), "Source collection must not be null.");
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
            var oldCollection = _collection;
            if (_collection.ContainsKey(source))
            {
                _collection[source].UpdateWeight(_weightManipulationStrategy, isSuccess);
                RaiseWeightUpdateEvent(_collection, oldCollection);
            }
        }

        public void UpdateResources(IReadOnlyDictionary<TSource, WeightItem> collection)
        {
            CheckCollectionArg(collection);

            ImmutableDictionary<TSource, WeightItem> oldCollection;
            ImmutableDictionary<TSource, WeightItem> newCollection;
            var isDifferent = false;
            lock (_collection)
            {
                oldCollection = _collection;
                newCollection = collection
                    .ToImmutableDictionary(
                        x => x.Key,
                        x => oldCollection.TryGetValue(x.Key, out var weight)
                            ? weight
                            : x.Value);
                isDifferent = !(
                    oldCollection.Keys.Count() == newCollection.Keys.Count() &&
                    oldCollection.Keys.All(x =>
                        newCollection.ContainsKey(x) &&
                        newCollection[x].Equals(oldCollection[x])));
                if (isDifferent)
                {
                    _collection = newCollection;
                }
            }
            if (isDifferent)
            {
                RaiseWeightUpdateEvent(oldCollection, newCollection);
            }
        }

        private void RaiseWeightUpdateEvent(ImmutableDictionary<TSource, WeightItem> oldCollection,
            ImmutableDictionary<TSource, WeightItem> newCollection)
        {
            RaiseOnUpdateWeight(oldCollection, newCollection);
            if (newCollection.Values.All(x => x.Weight == x.MinWeight))
            {
                RaiseOnAllSourcesReachBottom(oldCollection, newCollection);
            }
        }

        protected virtual void RaiseOnUpdateWeight(
                IReadOnlyDictionary<TSource, WeightItem> oldCollection,
                IReadOnlyDictionary<TSource, WeightItem> newCollection) =>
            OnUpdateWeight?.Invoke(
                this,
                new UpdateWeightEventArgs<TSource>(oldCollection, newCollection));

        protected virtual void RaiseOnAllSourcesReachBottom(
            IReadOnlyDictionary<TSource, WeightItem> oldCollection,
                IReadOnlyDictionary<TSource, WeightItem> newCollection) =>
            OnAllSourcesReachBottom?.Invoke(
                this,
                new UpdateWeightEventArgs<TSource>(oldCollection, newCollection));
    }
    
}
