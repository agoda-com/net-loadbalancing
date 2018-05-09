using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;

namespace Agoda.LoadBalancing.Test
{
    public class NoopWeightManipulationStrategyTest
    {
        [TestCase(false)]
        [TestCase(true)]
        public void UpdateWeight_Various(bool isSuccess)
        {
            var strats = NoopWeightManipulationStrategy.Default;
            var dict = new Dictionary<string, WeightItem>()
            {
                {"tgt", new WeightItem(50, 100)}
            };
            var newDict = strats.UpdateWeight(dict.ToImmutableDictionary(), "tgt", dict["tgt"], isSuccess);
            // Thanks to immutable collection that implements Equals correctly
            Assert.AreEqual(dict.ToImmutableDictionary(), newDict);
        }
    }
}
