using Moq;
using NUnit.Framework;

namespace Agoda.Frameworks.LoadBalancing.Test
{
    public class SplitWeightManipulationStrategyTest
    {
        [Test]
        public void UpdateWeight_Success()
        {
            var result = new WeightItem(10, 10);
            var inc = new Mock<IWeightManipulationStrategy>();
            inc.Setup(x => x.UpdateWeight(
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true))
                .Returns(result);
            var strats = new SplitWeightManipulationStrategy(inc.Object, Mock.Of<IWeightManipulationStrategy>());
            var newWeight = strats.UpdateWeight("tgt", new WeightItem(50, 100), true);
            Assert.AreEqual(result, newWeight);
        }

        [Test]
        public void UpdateWeight_Failure()
        {
            var result = new WeightItem(10, 10);
            var dec = new Mock<IWeightManipulationStrategy>();
            dec.Setup(x => x.UpdateWeight(
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false))
                .Returns(result);
            var strats = new SplitWeightManipulationStrategy(Mock.Of<IWeightManipulationStrategy>(), dec.Object);
            var newWeight = strats.UpdateWeight("tgt", new WeightItem(50, 100), false);
            Assert.AreEqual(result, newWeight);
        }
    }
}
