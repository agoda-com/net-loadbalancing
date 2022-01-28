namespace Agoda.Frameworks.LoadBalancing
{
    public interface IWeightManipulationStrategy
    {
        WeightItem UpdateWeight(
            WeightItem originalWeight,
            bool isSuccess);
    }
}
