namespace Agoda.Frameworks.LoadBalancing
{
    public interface IWeightManipulationStrategy
    {
        WeightItem UpdateWeight<T>(
            T source,
            WeightItem originalWeight,
            bool isSuccess);
    }
}
