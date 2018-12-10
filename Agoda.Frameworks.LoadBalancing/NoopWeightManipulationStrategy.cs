namespace Agoda.Frameworks.LoadBalancing
{
    public sealed class NoopWeightManipulationStrategy : IWeightManipulationStrategy
    {
        private NoopWeightManipulationStrategy()
        {
        }

        public static NoopWeightManipulationStrategy Default { get; }
            = new NoopWeightManipulationStrategy();

        public WeightItem UpdateWeight<T>(T source, WeightItem originalWeight, bool isSuccess)
        {
            return originalWeight;
        }
    }
}
