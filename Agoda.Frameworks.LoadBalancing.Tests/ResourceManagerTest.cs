using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Agoda.Frameworks.LoadBalancing.Test
{
    public class ResourceManagerTest
    {
        // TODO: Test ChooseRandomly?
        private ImmutableDictionary<string, WeightItem> _dict;
        private Mock<IWeightManipulationStrategy> _strats;
        private WeightItem _newWeightItem;

        [SetUp]
        public void SetUp()
        {
            _dict = new Dictionary<string, WeightItem>()
            {
                {"tgt", new WeightItem(10, 100)}
            }.ToImmutableDictionary();
            _newWeightItem = new WeightItem(50, 100);
            _strats = new Mock<IWeightManipulationStrategy>();
            _strats.Setup(x => x.UpdateWeight(
                    It.IsAny<WeightItem>(),
                    It.IsAny<bool>()))
                .Returns(() => _newWeightItem);
        }

        [Test]
        public void UpdateWeight_OnUpdateWeight_Unchanged()
        {
            var mgr = new ResourceManager<string>(
                _dict,
                _strats.Object);
            var onUpdateWeightCount = 0;
            mgr.OnUpdateWeight += (sender, args) =>
            {
                onUpdateWeightCount++;
            };

            mgr.UpdateWeight("tgt2", true);

            Assert.AreEqual(0, onUpdateWeightCount);
            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<WeightItem>(),
                    true),
                Times.Never);
        }

        [Test]
        public void UpdateWeight_OnUpdateWeight_Success()
        {
            var mgr = new ResourceManager<string>(_dict, _strats.Object);

            var onUpdateWeightCount = 0;
            mgr.OnUpdateWeight += (sender, args) =>
            {
                onUpdateWeightCount++;
                Assert.AreEqual(_newWeightItem, args.NewResources.Values.First());
            };

            mgr.UpdateWeight("tgt", true);

            Assert.AreEqual(1, onUpdateWeightCount);
            _strats.Verify(x => x.UpdateWeight(
                    _dict["tgt"],
                    true),
                Times.Once);
        }

        [Test]
        public void UpdateWeight_OnAllSourcesReachBottom_Failure()
        {
            _newWeightItem = new WeightItem(1, 100);
            var mgr = new ResourceManager<string>(_dict, _strats.Object);

            var onEventRaised = 0;
            mgr.OnAllSourcesReachBottom += (sender, args) =>
            {
                onEventRaised++;
                Assert.AreEqual(_newWeightItem, args.NewResources.Values.First());
            };

            mgr.UpdateWeight("tgt", false);

            Assert.AreEqual(1, onEventRaised);
            _strats.Verify(x => x.UpdateWeight(
                    _dict["tgt"],
                    false),
                Times.Once);
        }

        [Test]
        public void UpdateResources_Keep_Weight()
        {
            var oldDict = new Dictionary<string, WeightItem>()
            {
                {"remove", new WeightItem(10, 100)},
                {"keep", new WeightItem(20, 100)},
                {"keep_unchanged", new WeightItem(555, 555)},
            };
            var newDict = new Dictionary<string, WeightItem>()
            {
                {"add", new WeightItem(50, 100)},
                {"keep", new WeightItem(20, 100)},
                {"keep_unchanged", new WeightItem(100, 100)},
            };
            var mgr = new ResourceManager<string>(
                oldDict,
                Mock.Of<IWeightManipulationStrategy>());
            mgr.UpdateResources(newDict);

            // add
            Assert.AreEqual(newDict["add"], mgr.Resources["add"]);
            // keep
            Assert.AreEqual(oldDict["keep"], mgr.Resources["keep"]);
            // keep_unchanged
            Assert.AreNotEqual(newDict["keep_unchanged"], mgr.Resources["keep_unchanged"]);
            // remove
            Assert.IsFalse(mgr.Resources.ContainsKey("remove"));
        }

        [Test]
        public void UpdateResources_AgodaWeight()
        {
            var mgr = ResourceManager.Create(new[] { "remove", "keep", "keep_unchanged" });
            // Change weight
            mgr.UpdateWeight("keep", false);

            mgr.UpdateResources(new[] { "keep", "keep_unchanged", "add" });

            // add
            Assert.IsTrue(WeightItem.CreateDefaultItem().Equals(mgr.Resources["add"]));
            // keep
            Assert.AreEqual(WeightItem.CreateDefaultItem().MaxWeight, mgr.Resources["keep"].MaxWeight);
            Assert.AreNotEqual(WeightItem.CreateDefaultItem().Weight, mgr.Resources["keep"].Weight);
            // keep_unchanged
            Assert.IsTrue(WeightItem.CreateDefaultItem().Equals(mgr.Resources["keep_unchanged"]));
            // remove
            Assert.IsFalse(mgr.Resources.ContainsKey("remove"));
        }

        [Test]
        public void UpdateWeight_NoReplacementWhenNoWeightChange()
        {
            _newWeightItem = _dict["tgt"];
            var mgr = ResourceManager.Create(new[] { "tgt" });
            var onUpdateWeightCount = 0;
            mgr.OnUpdateWeight += (sender, args) =>
            {
                onUpdateWeightCount++;
            };

            var oldResources = mgr.Resources
                .ToDictionary(entry => entry.Key,
                            entry => entry.Value); 
            mgr.UpdateWeight("tgt", true);

            onUpdateWeightCount.ShouldBe(0);
            oldResources.ShouldBe(mgr.Resources.ToDictionary(entry => entry.Key,
                entry => entry.Value));
        }
        
        [Test]
        public void Create_ShouldWorkCorrectly()
        {
            var mgr = ResourceManager.Create(new[] { "tgt", "ccc", "ttt" });
            Assert.AreEqual(mgr.Resources.Count, 3);
        }

        [Test]
        public void Create_WithDuplicateItem_ShouldRemoveDuplicate()
        {
            var mgr = ResourceManager.Create(new[] { "tgt", "tgt", "ttt" });
            Assert.AreEqual(mgr.Resources.Count, 2);
        }

        [Test]
        public void UpdateWeight_ReplacementWhenWeightChange()
        {
            _newWeightItem = _dict["tgt"];
            var mgr = ResourceManager.Create(new[] { "tgt" });
            var onUpdateWeightCount = 0;
            mgr.OnUpdateWeight += (sender, args) =>
            {
                onUpdateWeightCount++;
            };

            var oldResources = mgr.Resources.ToDictionary(entry => entry.Key,
                entry => entry.Value); ;
            mgr.UpdateWeight("tgt", false);

            Assert.AreEqual(1, onUpdateWeightCount);
            Assert.AreNotSame(oldResources, mgr.Resources);
        }
    }
}
