using System;
using System.Collections.Immutable;

namespace Agoda.LoadBalancing
{
    public class ExponentialWeightManipulationStrategy : IWeightManipulationStrategy
    {
        public double Magnitude { get; }

        public ExponentialWeightManipulationStrategy(double magnitude)
        {
            if (magnitude <= 0)
            {
                throw new ArgumentException("Magnitude cannot be equal or lesser than 0.", nameof(magnitude));
            }
            Magnitude = magnitude;
        }

        public ImmutableDictionary<T, WeightItem> UpdateWeight<T>(
            ImmutableDictionary<T, WeightItem> collection,
            T source,
            WeightItem originalWeight,
            bool isSuccess)
        {
            var originialWeightValue = originalWeight.Weight;
            var newWeight = isSuccess
                ? originialWeightValue * Magnitude
                : originialWeightValue / Magnitude;
            // Convert.ToInt32 is Math.Round that returns int
            var delta = Convert.ToInt32(newWeight) - originialWeightValue;
            return collection.SetItem(source, originalWeight.SetNewWeight(delta));
        }
    }
}
