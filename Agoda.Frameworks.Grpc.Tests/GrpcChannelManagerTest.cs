using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agoda.Frameworks.Grpc.Tests.Proto;
using Grpc.Core;
using NUnit.Framework;

namespace Agoda.Frameworks.Grpc.Tests
{
    public class GrpcChannelManagerTest
    {
        [Test]
        public void TestUpdateResources()
        {
            var channelManager = new GrpcChannelManager(
                new string[] { "server1", "server2" },
                timeout: TimeSpan.FromMilliseconds(200));

            var channelsBeforeUpdate = channelManager
                .ResourceManager
                .Resources
                .Keys
                .ToDictionary(x => x.Url, x => x.Channel);
            Assert.AreEqual(2, channelManager.ResourceManager.Resources.Count);
            Assert.IsNotNull(channelsBeforeUpdate["server1"]);
            Assert.IsNotNull(channelsBeforeUpdate["server2"]);

            channelManager.UpdateResources(new string[] { "server1", "server3", "server4" });

            var channelsAfterUpdate = channelManager
                .ResourceManager
                .Resources
                .Keys
                .ToDictionary(x => x.Url, x => x.Channel);
            Assert.AreEqual(3, channelManager.ResourceManager.Resources.Count);
            Assert.IsNotNull(channelsAfterUpdate["server1"]);
            Assert.IsNotNull(channelsAfterUpdate["server3"]);
            Assert.IsNotNull(channelsAfterUpdate["server4"]);

            Assert.AreSame(channelsBeforeUpdate["server1"], channelsAfterUpdate["server1"]);
        }

        [Test]
        public void TestUpdateResourcesOnClient()
        {
            var server1 = SampleApiMock.CreateLocalServer(() =>
            {
                return Task.FromResult(new SampleResponse() { Payload = "server1" });
            });
            var server2 = SampleApiMock.CreateLocalServer(() =>
            {
                return Task.FromResult(new SampleResponse() { Payload = "server2" });
            });

            server1.Start();
            server2.Start();

            try
            {
                var server1Url = GrpcServerUtil.GetServerUrl(server1);
                var server2Url = GrpcServerUtil.GetServerUrl(server2);

                var channelManager = new GrpcChannelManager(
                    new string[] { server1Url },
                    timeout: TimeSpan.FromMilliseconds(200));
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                var result1 = client.SampleRpcMethod(new SampleRequest() { Payload = "" });
                Assert.AreEqual(result1.Payload, "server1");

                channelManager.UpdateResources(new string[] { server2Url });

                var result2 = client.SampleRpcMethod(new SampleRequest() { Payload = "" });
                Assert.AreEqual(result2.Payload, "server2");
            }
            finally
            {
                server1.ShutdownAsync().Wait();
                server2.ShutdownAsync().Wait();
            }
        }

        [Test]
        public void TestFailureOnRetryTransientError()
        {
            var attempt = 0;
            var server = SampleApiMock.CreateLocalServer(() =>
            {
                attempt++;
                return null;
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);

                var channelManager = new GrpcChannelManager(
                    new string[] { serverUrl },
                    timeout: TimeSpan.FromMilliseconds(200),
                    maxRetry: 3);
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                Assert.Throws<RpcException>(() => client.SampleRpcMethod(new SampleRequest() { Payload = "" }));
                Assert.AreEqual(3, attempt);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }

        [Test]
        public void TestFailureOnRetryTransientErrorAsync()
        {
            var attempt = 0;
            var server = SampleApiMock.CreateLocalServer(() =>
            {
                attempt++;
                return null;
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);

                var channelManager = new GrpcChannelManager(
                    new string[] { serverUrl },
                    timeout: TimeSpan.FromMilliseconds(200),
                    maxRetry: 3);
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                Assert.ThrowsAsync<RpcException>(async () => await client.SampleRpcMethodAsync(new SampleRequest() { Payload = "" }));
                Assert.AreEqual(3, attempt);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }

        [Test]
        public void TestSuccessOnRetry()
        {
            var attempt = 0;
            var server = SampleApiMock.CreateLocalServer(() =>
            {
                if (attempt == 2)
                {
                    return Task.FromResult(new SampleResponse() { Payload = "success!" });
                }
                else
                {
                    attempt++;
                    return null;
                }
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);

                var channelManager = new GrpcChannelManager(
                    new string[] { serverUrl },
                    timeout: TimeSpan.FromMilliseconds(200),
                    maxRetry: 3);
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                var result = client.SampleRpcMethod(new SampleRequest() { Payload = "" });

                Assert.AreEqual(2, attempt);
                Assert.AreEqual("success!", result.Payload);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }

        [Test]
        public async Task TestSuccessOnRetryAsync()
        {
            var attempt = 0;
            var server = SampleApiMock.CreateLocalServer(() =>
            {
                if (attempt == 2)
                {
                    return Task.FromResult(new SampleResponse() { Payload = "success!" });
                }
                else
                {
                    attempt++;
                    return null;
                }
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);

                var channelManager = new GrpcChannelManager(
                    new string[] { serverUrl },
                    timeout: TimeSpan.FromMilliseconds(200),
                    maxRetry: 3);
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                var result = await client.SampleRpcMethodAsync(new SampleRequest() { Payload = "" }).ResponseAsync;

                Assert.AreEqual(2, attempt);
                Assert.AreEqual("success!", result.Payload);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }

        [Test]
        public void TestRetryOnDeadlineExceeded()
        {
            var attempt = 0;
            var server = SampleApiMock.CreateLocalServer(async () =>
            {
                attempt++;
                await Task.Delay(5000);
                return new SampleResponse() { Payload = "" };
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);
                var channelManager = new GrpcChannelManager(
                    new string[] { serverUrl },
                    timeout: TimeSpan.FromMilliseconds(200),
                    maxRetry: 3);
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                var e = Assert.Throws<RpcException>(() => client.SampleRpcMethod(new SampleRequest() { Payload = "" }));
                Assert.IsTrue(e is RpcException);
                Assert.AreEqual((e as RpcException).StatusCode, StatusCode.DeadlineExceeded);
                Assert.AreEqual(3, attempt);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }

        [Test]
        public void TestRetryOnDeadlineExceededAsync()
        {
            var attempt = 0;
            var server = SampleApiMock.CreateLocalServer(async () =>
            {
                attempt++;
                await Task.Delay(5000);
                return new SampleResponse() { Payload = "" };
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);
                var channelManager = new GrpcChannelManager(
                    new string[] { serverUrl },
                    timeout: TimeSpan.FromMilliseconds(200),
                    maxRetry: 3);
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                var e = Assert.ThrowsAsync<RpcException>(async () => await client.SampleRpcMethodAsync(new SampleRequest() { Payload = "" }));
                Assert.AreEqual(e.StatusCode, StatusCode.DeadlineExceeded);
                Assert.AreEqual(3, attempt);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }

        [Test]
        public void TestNullTimeoutNotFail()
        {
            var server = SampleApiMock.CreateLocalServer(() =>
            {
                return Task.FromResult(new SampleResponse() { Payload = "success" });
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);
                var channelManager = new GrpcChannelManager(
                    new string[] { serverUrl },
                    timeout: null,
                    maxRetry: 3);
                var lbCallInvoker = channelManager.GetCallInvoker();
                var client = new SampleApi.SampleApiClient(lbCallInvoker);

                var result = client.SampleRpcMethod(new SampleRequest() { Payload = "" });
                Assert.AreEqual("success", result.Payload);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }   
        }

        [Test]
        public void TestOnError()
        {
            var channelManager = new GrpcChannelManager(new string[] { "randomhost" }, TimeSpan.FromMilliseconds(10), maxRetry: 3);
            var callInvoker = channelManager.GetCallInvoker();
            var client = new SampleApi.SampleApiClient(callInvoker);

            var attemptList = new List<int>();
            var errorList = new List<Exception>();
            channelManager.OnError += (obj, args) =>
            {
                attemptList.Add(args.AttemptCount);
                errorList.Add(args.Error);
            };

            Assert.Throws<RpcException>(() => client.SampleRpcMethod(new SampleRequest() { Payload = "" }));
            Assert.AreEqual(attemptList, new List<int>() { 1, 2, 3 });
            Assert.IsTrue(errorList.All(e => e is RpcException));
        }

        [Test]
        public void TestOnErrorAsync()
        {
            var channelManager = new GrpcChannelManager(new string[] { "randomhost" }, TimeSpan.FromMilliseconds(10), maxRetry: 3);
            var callInvoker = channelManager.GetCallInvoker();
            var client = new SampleApi.SampleApiClient(callInvoker);

            var attemptList = new List<int>();
            var errorList = new List<Exception>();
            channelManager.OnError += (obj, args) =>
            {
                attemptList.Add(args.AttemptCount);
                errorList.Add(args.Error);
            };

            Assert.ThrowsAsync<RpcException>(async () => await client.SampleRpcMethodAsync(new SampleRequest() { Payload = "" }));
            Assert.AreEqual(attemptList, new List<int>() { 1, 2, 3 });
            Assert.IsTrue(errorList.All(e => e is RpcException));
        }

    }
}
