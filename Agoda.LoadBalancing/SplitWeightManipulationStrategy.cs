using System.Collections.Immutable;

namespace Agoda.LoadBalancing
{
    public class SplitWeightManipulationStrategy : IWeightManipulationStrategy
    {
        public IWeightManipulationStrategy Increment { get; }
        public IWeightManipulationStrategy Decrement { get; }

        public SplitWeightManipulationStrategy(
            IWeightManipulationStrategy increment,
            IWeightManipulationStrategy decrement)
        {
            Increment = increment;
            Decrement = decrement;
        }

        public ImmutableDictionary<T, WeightItem> UpdateWeight<T>(
            ImmutableDictionary<T, WeightItem> collection,
            T source,
            WeightItem originalWeight,
            bool isSuccess)
        {
            var strategy = isSuccess ? Increment : Decrement;
            return strategy.UpdateWeight(collection, source, originalWeight, isSuccess);
        }
    }
}
