using System.Collections.Immutable;

namespace Agoda.LoadBalancing
{
    public interface IWeightManipulationStrategy
    {
        ImmutableDictionary<T, WeightItem> UpdateWeight<T>(
            ImmutableDictionary<T, WeightItem> collection,
            T source,
            WeightItem originalWeight,
            bool isSuccess);
    }
}
