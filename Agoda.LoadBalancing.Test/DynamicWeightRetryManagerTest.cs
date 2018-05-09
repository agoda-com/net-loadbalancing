using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Agoda.LoadBalancing.Test
{
    public class DynamicWeightRetryManagerTest
    {
        // TODO: Test RandomSelect function?
        private ImmutableDictionary<string, WeightItem> _dict;
        private Mock<IWeightManipulationStrategy> _strats;

        [SetUp]
        public void SetUp()
        {
            _dict = new Dictionary<string, WeightItem>()
            {
                {"tgt", new WeightItem(50, 100)}
            }.ToImmutableDictionary();

            _strats = new Mock<IWeightManipulationStrategy>();
            _strats.Setup(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    It.IsAny<string>(),
                    It.IsAny<WeightItem>(),
                    It.IsAny<bool>()))
                .Returns(_dict);
        }

        [Test]
        public void Collection_No_Source()
        {
            var mgr = new DynamicWeightRetryManager<string>(
                new Dictionary<string, WeightItem>()
                {
                    {"tgt", new WeightItem(50, 100)}
                },
                Mock.Of<IWeightManipulationStrategy>(),
                (_1, _2) => true);

            Assert.AreEqual(50, mgr.Collection.First().Weight);
        }

        [Test]
        public void ExecuteAction_Return()
        {
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (_1, _2) => true);

            var result = mgr.ExecuteAction((str, _) => str);
            Assert.AreEqual("tgt", result);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Once);
        }

        [Test]
        public void ExecuteAction_Retry_And_Success()
        {
            var failUntilCount = 3;
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (retryCount, exception) =>
                {
                    Assert.LessOrEqual(retryCount, failUntilCount);
                    Assert.IsInstanceOf<InvalidOperationException>(exception);

                    return true;
                });
            var result = mgr.ExecuteAction((_, retryCount) =>
            {
                if (retryCount < failUntilCount)
                {
                    throw new InvalidOperationException();
                }
                return retryCount;
            });
            Assert.AreEqual(failUntilCount, result);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false),
                Times.Exactly(failUntilCount));
            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Once);
        }

        [Test]
        public void ExecuteAction_Retry_And_Fail()
        {
            var maxRetryCount = 3;
            var outerRetryCount = -1;
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (retryCount, _) =>
                {
                    outerRetryCount = retryCount;
                    return retryCount < maxRetryCount;
                });

            Assert.Throws<InvalidOperationException>(() =>
            {
                mgr.ExecuteAction<int>((_1, _2) => throw new InvalidOperationException());
            });
            Assert.AreEqual(maxRetryCount, outerRetryCount);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false),
                Times.Exactly(maxRetryCount));
            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Never);
        }

        [Test]
        public void ExecuteAction_Retry_And_Fail_First_Attempt()
        {
            var maxRetryCount = 0;
            var outerRetryCount = -1;
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (retryCount, _) =>
                {
                    outerRetryCount = retryCount;
                    return retryCount < maxRetryCount;
                });

            Assert.Throws<InvalidOperationException>(() =>
            {
                mgr.ExecuteAction<int>((_1, _2) => throw new InvalidOperationException());
            });
            Assert.AreEqual(1, outerRetryCount);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false),
                Times.Once);
            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_Return()
        {
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (_1, _2) => true);

            var result = await mgr.ExecuteAsync((str, _) => Task.FromResult(str));
            Assert.AreEqual("tgt", result);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_Retry_And_Success()
        {
            var failUntilCount = 3;
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (retryCount, exception) =>
                {
                    Assert.LessOrEqual(retryCount, failUntilCount);
                    Assert.IsInstanceOf<InvalidOperationException>(exception);

                    return true;
                });
            var result = await mgr.ExecuteAsync((_, retryCount) =>
            {
                if (retryCount < failUntilCount)
                {
                    return Task.FromException<int>(new InvalidOperationException());
                }
                return Task.FromResult(retryCount);
            });
            Assert.AreEqual(failUntilCount, result);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false),
                Times.Exactly(failUntilCount));
            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Once);
        }

        [Test]
        public void ExecuteAsync_Retry_And_Fail()
        {
            var maxRetryCount = 3;
            var outerRetryCount = -1;
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (retryCount, _) =>
                {
                    outerRetryCount = retryCount;
                    return retryCount < maxRetryCount;
                });

            Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return mgr.ExecuteAsync<int>((_1, _2) => throw new InvalidOperationException());
            });
            Assert.AreEqual(maxRetryCount, outerRetryCount);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false),
                Times.Exactly(maxRetryCount));
            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Never);
        }

        [Test]
        public void ExecuteAsync_Retry_And_Fail_First_Attempt()
        {
            var maxRetryCount = 0;
            var outerRetryCount = -1;
            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (retryCount, _) =>
                {
                    outerRetryCount = retryCount;
                    return retryCount < maxRetryCount;
                });

            Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return mgr.ExecuteAsync<int>((_1, _2) => throw new InvalidOperationException());
            });
            Assert.AreEqual(1, outerRetryCount);

            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    false),
                Times.Once);
            _strats.Verify(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    "tgt",
                    It.IsAny<WeightItem>(),
                    true),
                Times.Never);
        }

        [Test]
        public void OnUpdateWeight_Success()
        {
            var newWeightItem = new WeightItem(100, 100);
            _strats.Setup(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    It.IsAny<string>(),
                    It.IsAny<WeightItem>(),
                    It.IsAny<bool>()))
                .Returns(new Dictionary<string, WeightItem>()
                {
                    {"tgt", newWeightItem}
                }.ToImmutableDictionary());

            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (_1, _2) => true);
            var onUpdateWeightCount = 0;
            mgr.OnUpdateWeight += (sender, args) =>
            {
                onUpdateWeightCount++;
                Assert.AreEqual(newWeightItem, args.WeightItems.First());
            };

            mgr.ExecuteAction((str, _) => str);

            Assert.AreEqual(1, onUpdateWeightCount);
        }

        [Test]
        public void OnAllSourcesReachBottom_Failure()
        {
            var newWeightItem = new WeightItem(1, 100, 1);
            _strats.Setup(x => x.UpdateWeight(
                    It.IsAny<ImmutableDictionary<string, WeightItem>>(),
                    It.IsAny<string>(),
                    It.IsAny<WeightItem>(),
                    It.IsAny<bool>()))
                .Returns(new Dictionary<string, WeightItem>()
                {
                    {"tgt", newWeightItem}
                }.ToImmutableDictionary());

            var mgr = new DynamicWeightRetryManager<string>(
                _dict,
                _strats.Object,
                (_1, _2) => true);
            var onEventRaised = 0;
            mgr.OnAllSourcesReachBottom += (sender, args) =>
            {
                onEventRaised++;
                Assert.AreEqual(newWeightItem, args.WeightItems.First());
            };

            mgr.ExecuteAction((str, _) => str);

            Assert.AreEqual(1, onEventRaised);
        }
    }
}
