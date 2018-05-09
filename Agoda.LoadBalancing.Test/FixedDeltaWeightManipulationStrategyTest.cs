using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;

namespace Agoda.LoadBalancing.Test
{
    public class FixedDeltaWeightManipulationStrategyTest
    {
        [Test]
        public void UpdateWeight_Success()
        {
            var strats = new FixedDeltaWeightManipulationStrategy(10);
            var dict = new Dictionary<string, WeightItem>()
            {
                {"tgt", new WeightItem(50, 100)}
            };
            var newDict = strats.UpdateWeight(dict.ToImmutableDictionary(), "tgt", dict["tgt"], true);
            Assert.AreEqual(60, newDict["tgt"].Weight);
        }

        [Test]
        public void UpdateWeight_Failure()
        {
            var strats = new FixedDeltaWeightManipulationStrategy(10);
            var dict = new Dictionary<string, WeightItem>()
            {
                {"tgt", new WeightItem(50, 100)}
            };
            var newDict = strats.UpdateWeight(dict.ToImmutableDictionary(), "tgt", dict["tgt"], false);
            Assert.AreEqual(40, newDict["tgt"].Weight);
        }
    }
}
