using NUnit.Framework;

namespace Agoda.Frameworks.LoadBalancing.Test
{
    public class FixedDeltaWeightManipulationStrategyTest
    {
        [Test]
        public void UpdateWeight_Success()
        {
            var strats = new FixedDeltaWeightManipulationStrategy(10);
            var newWeight = strats.UpdateWeight("tgt", new WeightItem(50, 100), true);
            Assert.AreEqual(60, newWeight.Weight);
        }

        [Test]
        public void UpdateWeight_Failure()
        {
            var strats = new FixedDeltaWeightManipulationStrategy(10);
            var newWeight = strats.UpdateWeight("tgt", new WeightItem(50, 100), false);
            Assert.AreEqual(40, newWeight.Weight);
        }
    }
}
