using NUnit.Framework;

namespace Agoda.Frameworks.LoadBalancing.Test
{
    public class WeightItemTest
    {
        [Test]
        public void SetNewWeight_Copy()
        {
            var weight = new WeightItem(50, 100);
            var newWeight = weight.SetNewWeight(20);
            
            // Delta
            Assert.AreEqual(70, newWeight.Weight);
            // Copy MaxWeight
            Assert.AreEqual(100, newWeight.MaxWeight);

            // Do not mutate original weight
            Assert.AreEqual(50, weight.Weight);
            Assert.AreEqual(100, weight.MaxWeight);
        }

        [Test]
        public void SetNewWeight_Max_Boundary()
        {
            var weight = new WeightItem(90, 100);
            var newWeight = weight.SetNewWeight(11);
            Assert.AreEqual(100, newWeight.Weight);
        }

        [Test]
        public void SetNewWeight_Min_Boundary()
        {
            var weight = new WeightItem(2, 100);
            var newWeight = weight.SetNewWeight(-2);
            Assert.AreEqual(1, newWeight.Weight);
        }
    }
}
