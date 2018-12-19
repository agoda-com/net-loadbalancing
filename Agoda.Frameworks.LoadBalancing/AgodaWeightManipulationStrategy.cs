using System;

namespace Agoda.Frameworks.LoadBalancing
{
    public class AgodaWeightManipulationStrategy : IWeightManipulationStrategy
    {
        private readonly IWeightManipulationStrategy _strategy = new SplitWeightManipulationStrategy(
            new FixedDeltaWeightManipulationStrategy(1000),
            new ExponentialWeightManipulationStrategy(100)
        );

        public WeightItem UpdateWeight<T>(T source, WeightItem originalWeight, bool isSuccess)
        {
            return _strategy.UpdateWeight(source, originalWeight, isSuccess);
        }
    }
}
