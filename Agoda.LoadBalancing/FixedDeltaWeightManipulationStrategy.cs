using System.Collections.Immutable;

namespace Agoda.LoadBalancing
{
    public class FixedDeltaWeightManipulationStrategy : IWeightManipulationStrategy
    {
        public int Delta { get; }

        public FixedDeltaWeightManipulationStrategy(int delta)
        {
            Delta = delta;
        }

        public ImmutableDictionary<T, WeightItem> UpdateWeight<T>(
            ImmutableDictionary<T, WeightItem> collection,
            T source,
            WeightItem originalWeight,
            bool isSuccess)
        {
            var delta = Delta * (isSuccess ? 1 : -1);
            return collection.SetItem(source, originalWeight.SetNewWeight(delta));
        }
    }
}
