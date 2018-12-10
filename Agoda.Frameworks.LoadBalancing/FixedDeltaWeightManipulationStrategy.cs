namespace Agoda.Frameworks.LoadBalancing
{
    public class FixedDeltaWeightManipulationStrategy : IWeightManipulationStrategy
    {
        public int Delta { get; }

        public FixedDeltaWeightManipulationStrategy(int delta)
        {
            Delta = delta;
        }

        public WeightItem UpdateWeight<T>(T source, WeightItem originalWeight, bool isSuccess)
        {
            var delta = Delta * (isSuccess ? 1 : -1);
            return originalWeight.SetNewWeight(delta);
        }
    }
}
