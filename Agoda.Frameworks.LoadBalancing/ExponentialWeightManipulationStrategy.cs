using System;

namespace Agoda.Frameworks.LoadBalancing
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

        public WeightItem UpdateWeight(WeightItem originalWeight, bool isSuccess)
        {
            var originialWeightValue = originalWeight.Weight;
            var newWeight = isSuccess
                ? originialWeightValue * Magnitude
                : originialWeightValue / Magnitude;
            // Convert.ToInt32 is Math.Round that returns int
            var delta = Convert.ToInt32(newWeight) - originialWeightValue;
            return originalWeight.SetNewWeight(delta);
        }
    }
}
