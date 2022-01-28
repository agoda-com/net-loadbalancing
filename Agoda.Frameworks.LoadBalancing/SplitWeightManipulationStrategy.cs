namespace Agoda.Frameworks.LoadBalancing
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

        public WeightItem UpdateWeight(WeightItem originalWeight, bool isSuccess)
        {
            var strategy = isSuccess ? Increment : Decrement;
            return strategy.UpdateWeight(originalWeight, isSuccess);
        }
    }
}
