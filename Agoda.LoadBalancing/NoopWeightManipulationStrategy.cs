using System.Collections.Immutable;

namespace Agoda.LoadBalancing
{
    public sealed class NoopWeightManipulationStrategy : IWeightManipulationStrategy
    {
        private NoopWeightManipulationStrategy()
        {
        }

        public static NoopWeightManipulationStrategy Default { get; }
            = new NoopWeightManipulationStrategy();

        public ImmutableDictionary<T, WeightItem> UpdateWeight<T>(
            ImmutableDictionary<T, WeightItem> collection,
            T source,
            WeightItem originalWeight,
            bool isSuccess)
        {
            return collection;
        }
    }
}
