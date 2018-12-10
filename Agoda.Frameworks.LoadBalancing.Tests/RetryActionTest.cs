using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Agoda.Frameworks.LoadBalancing.Test
{
    // TODO: Test onError
    public class RetryActionTest
    {
        private List<bool> _updateWeightSequence;

        private void UpdateWeight(string str, bool isSuccess)
        {
            _updateWeightSequence.Add(isSuccess);
            Assert.AreEqual("foo", str);
        }

        [SetUp]
        public void SetUp()
        {
            _updateWeightSequence = new List<bool>();
        }

        [Test]
        public void ExecuteAction_Return()
        {
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            var actionCount = 0;
            var retryCount = 0;
            var result = mgr.ExecuteAction((str, _) =>
            {
                actionCount++;
                return str;
            }, (_1, _2) =>
            {
                retryCount++;
                return true;
            });

            Assert.AreEqual("foo", result);

            Assert.AreEqual(1, _updateWeightSequence.Count);
            Assert.IsTrue(_updateWeightSequence[0]);

            Assert.AreEqual(1, actionCount);
            Assert.AreEqual(0, retryCount);
        }

        [Test]
        public void ExecuteAction_Retry_And_Success()
        {
            var failUntilCount = 3;
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            var result = mgr.ExecuteAction((str, attemptCount) =>
            {
                if (attemptCount < failUntilCount)
                {
                    throw new InvalidOperationException();
                }
                return attemptCount;
            }, (attempt, exception) =>
            {
                Assert.LessOrEqual(attempt, failUntilCount);
                Assert.IsInstanceOf<InvalidOperationException>(exception);

                return true;
            });

            Assert.AreEqual(failUntilCount, result);

            Assert.AreEqual(failUntilCount, _updateWeightSequence.Count);
            Assert.IsFalse(_updateWeightSequence[0]);
            Assert.IsFalse(_updateWeightSequence[1]);
            Assert.IsTrue(_updateWeightSequence[2]);
        }

        [Test]
        public void ExecuteAction_Retry_And_Fail()
        {
            var outerRetryCount = -1;
            var maxRetryCount = 3;
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            Assert.Throws<InvalidOperationException>(() =>
            {
                mgr.ExecuteAction<int>((str, attemptCount) =>
                {
                    throw new InvalidOperationException();
                }, (attempt, exception) =>
                {
                    outerRetryCount = attempt;
                    return attempt < maxRetryCount;
                });
            });

            Assert.AreEqual(maxRetryCount, outerRetryCount);
            Assert.AreEqual(maxRetryCount, _updateWeightSequence.Count);
            Assert.IsFalse(_updateWeightSequence[0]);
            Assert.IsFalse(_updateWeightSequence[1]);
            Assert.IsFalse(_updateWeightSequence[2]);
        }

        [Test]
        public void ExecuteAction_Retry_And_Fail_First_Attempt()
        {
            var outerRetryCount = -1;
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            Assert.Throws<InvalidOperationException>(() =>
            {
                mgr.ExecuteAction<int>((str, attemptCount) =>
                {
                    throw new InvalidOperationException();
                }, (attempt, exception) =>
                {
                    outerRetryCount = attempt;
                    return false;
                });
            });

            Assert.AreEqual(1, outerRetryCount);
            Assert.AreEqual(1, _updateWeightSequence.Count);
            Assert.IsFalse(_updateWeightSequence[0]);
        }

        [Test]
        public void ExecuteAction_Deny_Async_Result()
        {
            var errMsg = "Async action should be executed with ExecuteAsync.";
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);
            Assert.Throws<ArgumentException>(
                () => mgr.ExecuteAction<Task>((_1, _2) => Task.FromResult("Task"), (_1, _2) => true),
                errMsg);
            Assert.Throws<ArgumentException>(
                () => mgr.ExecuteAction<Task<string>>((_1, _2) => Task.FromResult("Task<T>"), (_1, _2) => true),
                errMsg);
        }

        [Test]
        public async Task ExecuteAsync_Return()
        {
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            var retryCount = 0;
            var result = await mgr.ExecuteAsync(
                (str, _) => Task.FromResult(str),
                (_1, _2) =>
                {
                    retryCount++;
                    return true;
                });

            Assert.AreEqual("foo", result);

            Assert.AreEqual(1, _updateWeightSequence.Count);
            Assert.IsTrue(_updateWeightSequence[0]);

            Assert.AreEqual(0, retryCount);
        }

        [Test]
        public async Task ExecuteAsync_Retry_And_Success()
        {
            var failUntilCount = 3;
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            var result = await mgr.ExecuteAsync(
                (str, attemptCount) =>
                {
                    if (attemptCount < failUntilCount)
                    {
                        return Task.FromException<int>(new InvalidOperationException());
                    }

                    return Task.FromResult(attemptCount);
                }, (attempt, exception) =>
                {
                    Assert.LessOrEqual(attempt, failUntilCount);
                    Assert.IsInstanceOf<InvalidOperationException>(exception);

                    return true;
                });

            Assert.AreEqual(failUntilCount, result);

            Assert.AreEqual(failUntilCount, _updateWeightSequence.Count);
            Assert.IsFalse(_updateWeightSequence[0]);
            Assert.IsFalse(_updateWeightSequence[1]);
            Assert.IsTrue(_updateWeightSequence[2]);
        }

        [Test]
        public void ExecuteAsync_Retry_And_Fail()
        {
            var outerRetryCount = -1;
            var maxRetryCount = 3;
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return mgr.ExecuteAsync<int>((str, attemptCount) =>
                {
                    throw new InvalidOperationException();
                }, (attempt, exception) =>
                {
                    outerRetryCount = attempt;
                    return attempt < maxRetryCount;
                });
            });

            Assert.AreEqual(maxRetryCount, outerRetryCount);
            Assert.AreEqual(maxRetryCount, _updateWeightSequence.Count);
            Assert.IsFalse(_updateWeightSequence[0]);
            Assert.IsFalse(_updateWeightSequence[1]);
            Assert.IsFalse(_updateWeightSequence[2]);
        }

        [Test]
        public void ExecuteAsync_Retry_And_Fail_First_Attempt()
        {
            var outerRetryCount = -1;
            var mgr = new RetryAction<string>(() => "foo", UpdateWeight);

            Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return mgr.ExecuteAsync<int>((str, attemptCount) =>
                {
                    throw new InvalidOperationException();
                }, (attempt, exception) =>
                {
                    outerRetryCount = attempt;
                    return false;
                });
            });

            Assert.AreEqual(1, outerRetryCount);
            Assert.AreEqual(1, _updateWeightSequence.Count);
            Assert.IsFalse(_updateWeightSequence[0]);
        }
    }
}
