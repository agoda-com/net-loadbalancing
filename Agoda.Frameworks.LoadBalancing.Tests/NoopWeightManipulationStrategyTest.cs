using NUnit.Framework;

namespace Agoda.Frameworks.LoadBalancing.Test
{
    public class NoopWeightManipulationStrategyTest
    {
        [TestCase(false)]
        [TestCase(true)]
        public void UpdateWeight_Various(bool isSuccess)
        {
            var strats = NoopWeightManipulationStrategy.Default;
            var oldWeight = new WeightItem(50, 100);
            var newWeight = strats.UpdateWeight(oldWeight, isSuccess);
            Assert.AreEqual(oldWeight, newWeight);
        }
    }
}
