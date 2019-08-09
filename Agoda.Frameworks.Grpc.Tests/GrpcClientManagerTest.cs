using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Agoda.Frameworks.Grpc.Tests.Proto;
using Grpc.Core;
using NUnit.Framework;

namespace Agoda.Frameworks.Grpc.Tests
{
    public class GrpcClientManagerTest
    {
        [Test]
        public void TestUpdateResources()
        {
            var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(
                new string[] { "server1", "server2" });

            var clientsBeforeUpdate = clientManager
                .ResourceManager
                .Resources
                .Keys
                .ToDictionary(x => x.Url, x => x.Client);
            Assert.AreEqual(2, clientManager.ResourceManager.Resources.Count);
            Assert.IsNotNull(clientsBeforeUpdate["server1"]);
            Assert.IsNotNull(clientsBeforeUpdate["server2"]);

            clientManager.UpdateResources(new string[] { "server1", "server3", "server4" });

            var clientsAfterUpdate = clientManager
                .ResourceManager
                .Resources
                .Keys
                .ToDictionary(x => x.Url, x => x.Client);
            Assert.AreEqual(3, clientManager.ResourceManager.Resources.Count);
            Assert.IsNotNull(clientsAfterUpdate["server1"]);
            Assert.IsNotNull(clientsAfterUpdate["server3"]);
            Assert.IsNotNull(clientsAfterUpdate["server4"]);

            Assert.AreSame(clientsBeforeUpdate["server1"], clientsAfterUpdate["server1"]);
        }

        [Test]
        public void TestUpdateResourcesOnProxiedClient()
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

                var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(new string[] { server1Url });
                var client = clientManager.GetClient();

                var result1 = client.SampleRpcMethod(new SampleRequest() { Payload = "" });
                Assert.AreEqual(result1.Payload, "server1");

                clientManager.UpdateResources(new string[] { server2Url });

                var result2 = client.SampleRpcMethod(new SampleRequest() { Payload = "" });
                Assert.AreEqual(result2.Payload, "server2");
            }
            finally
            {
                server1.KillAsync().Wait();
                server2.KillAsync().Wait();
            }
        }

        [Test]
        public void TestRetryOnTransientError()
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
                var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(
                    new string[] { serverUrl },
                    maxRetry: 3);

                var client = clientManager.GetClient();

                try
                {
                    client.SampleRpcMethod(new SampleRequest() { Payload = "test" });
                }
                catch (Exception)
                {
                }

                Assert.AreEqual(3, attempt);
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }

        [Test]
        public void TestExceptionOnFailure()
        {
            var server = SampleApiMock.CreateLocalServer(async () =>
            {
                await Task.Delay(100);
                return new SampleResponse() { Payload = "" };
            });
            server.Start();

            try
            {
                var serverUrl = GrpcServerUtil.GetServerUrl(server);
                var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(new string[] { serverUrl });
                var client = clientManager.GetClient();

                try
                {
                    var deadline = DateTime.UtcNow.AddMilliseconds(20);
                    client.SampleRpcMethod(new SampleRequest() { Payload = "" }, deadline: deadline);
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is TargetInvocationException);
                    Assert.IsTrue(e.InnerException is RpcException);
                }
            }
            finally
            {
                server.ShutdownAsync().Wait();
            }
        }
    }
}
