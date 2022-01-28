using System;
using System.Threading;

namespace Agoda.Frameworks.LoadBalancing
{
    public sealed class WeightItem
    {
        private volatile int _weight;

        public WeightItem(int weight, int maxWeight)
            : this(weight, maxWeight, 1)
        {
        }

        public WeightItem(int weight, int maxWeight, int minWeight)
        {
            if (minWeight < 1)
            {
                throw new ArgumentException("minWeight must be equal or greater than 1", nameof(minWeight));
            }
            if (weight < minWeight)
            {
                throw new ArgumentException("weight must be equal or greater than minWeight", nameof(weight));
            }
            if (weight > maxWeight)
            {
                throw new ArgumentException("weight must be equal or lesser than maxWeight", nameof(weight));
            }

            MaxWeight = maxWeight;
            MinWeight = minWeight;
            _weight = weight;
        }

        public int Weight => _weight;
        public int MinWeight { get; }
        public int MaxWeight { get; }

        public override bool Equals(object obj)
        {
            return obj is WeightItem item && Equals(item);
        }

        private bool Equals(WeightItem other)
        {
            return Weight == other.Weight &&
                   MinWeight == other.MinWeight &&
                   MaxWeight == other.MaxWeight;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Weight;
                hashCode = (hashCode * 397) ^ MinWeight;
                hashCode = (hashCode * 397) ^ MaxWeight;
                return hashCode;
            }
        }

        public static WeightItem CreateDefaultItem()
        {
            return new WeightItem(1000, 1000);
        }

        public bool UpdateWeight(IWeightManipulationStrategy weightManipulationStrategy, bool isSuccess)
        {
            var initialWeight = _weight;
            var newWeight = weightManipulationStrategy.UpdateWeight(this, isSuccess).Weight;
            if (newWeight == initialWeight) return false;

            Interlocked.CompareExchange(ref _weight, newWeight, initialWeight);
            return true;

        }
    }

    public static class WeightItemExtension
    {
        public static WeightItem SetNewWeight(this WeightItem weight, int delta)
        {
            var newWeight = Math.Min(Math.Max(weight.Weight + delta, weight.MinWeight), weight.MaxWeight);
            return new WeightItem(newWeight, weight.MaxWeight, weight.MinWeight);
        }
    }
}
