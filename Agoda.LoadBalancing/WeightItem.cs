using System;

namespace Agoda.LoadBalancing
{
    public sealed class WeightItem
    {
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
                throw new ArgumentException("weight must be equal or greater than minWeight", nameof(minWeight));
            }
            if (weight > maxWeight)
            {
                throw new ArgumentException("weight must be equal or lesser than maxWeight", nameof(minWeight));
            }

            Weight = weight;
            MaxWeight = maxWeight;
            MinWeight = minWeight;
        }

        public int Weight { get; }
        public int MinWeight { get; }
        public int MaxWeight { get; }
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
