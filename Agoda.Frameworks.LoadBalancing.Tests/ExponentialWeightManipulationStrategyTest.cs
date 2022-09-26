using NUnit.Framework;

namespace Agoda.Frameworks.LoadBalancing.Test
{
    public class ExponentialWeightManipulationStrategyTest
    {
        [Test]
        public void UpdateWeight_Success()
        {
            var strats = new ExponentialWeightManipulationStrategy(1.5);
            var newWeight = strats.UpdateWeight(new WeightItem(50, 100), true);
            Assert.AreEqual(75, newWeight.Weight);
        }

        [Test]
        public void UpdateWeight_Failure()
        {
            var strats = new ExponentialWeightManipulationStrategy(2);
            var newWeight = strats.UpdateWeight(new WeightItem(50, 100), false);
            Assert.AreEqual(25, newWeight.Weight);
        }
    }
}
