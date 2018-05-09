using System.Collections.Generic;
using System.Collections.Immutable;
using Moq;
using NUnit.Framework;

namespace Agoda.LoadBalancing.Test
{
    public class SplitWeightManipulationStrategyTest
    {
        [Test]
        public void UpdateWeight_Success()
        {
            var result = ImmutableDictionary.Create<string, WeightItem>();
            var inc = new Mock<IWeightManipulationStrategy>();
            inc.Setup(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true))
                .Returns(result);
            var strats = new SplitWeightManipulationStrategy(inc.Object, Mock.Of<IWeightManipulationStrategy>());
            var dict = new Dictionary<string, WeightItem>()
            {
                {"tgt", new WeightItem(50, 100)}
            };
            var newDict = strats.UpdateWeight(dict.ToImmutableDictionary(), "tgt", dict["tgt"], true);
            Assert.AreEqual(result, newDict);
        }

        [Test]
        public void UpdateWeight_Failure()
        {
            var result = ImmutableDictionary.Create<string, WeightItem>();
            var dec = new Mock<IWeightManipulationStrategy>();
            dec.Setup(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false))
                .Returns(result);
            var strats = new SplitWeightManipulationStrategy(Mock.Of<IWeightManipulationStrategy>(), dec.Object);
            var dict = new Dictionary<string, WeightItem>()
            {
                {"tgt", new WeightItem(50, 100)}
            };
            var newDict = strats.UpdateWeight(dict.ToImmutableDictionary(), "tgt", dict["tgt"], false);
            Assert.AreEqual(result, newDict);
        }
    }
}
