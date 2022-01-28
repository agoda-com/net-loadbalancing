using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Agoda.Frameworks.Http;
using Agoda.Frameworks.Http.AutoRestExt;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace Agoda.Frameworks.LoadBalancing.Tests
{
    public class RrCompatibleHttpClientTest
    {
        [Test]
        public async Task TestExecuteAsync()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Get, "http://test/*")
                .Respond(msg =>
                {
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    return new StringContent("ok");
                });

            var client = new RrCompatibleHttpClient(
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                mockHttp);
            var res = await client.ExecuteAsync(HttpMethod.Get, "api/55", null, null);
            Assert.AreEqual("ok", res.Results);
            Assert.IsTrue(res.IsOK);
            Assert.IsFalse(res.IsScala);
        }

        [Test]
        public async Task TestExecuteAsyncPost()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Post, "http://test/*")
                .With(arg =>
                {
                    // Must provide content-type
                    return arg.Content.Headers.ContentType.MediaType == "application/json";
                })
                .WithContent("{\"foo\":\"bar\"}")
                .Respond(msg =>
                {
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    return new StringContent("ok");
                });

            var client = new RrCompatibleHttpClient(
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                mockHttp);
            var res = await client.ExecuteAsync(HttpMethod.Post, "api/55", "{\"foo\":\"bar\"}", null);
            Assert.AreEqual("ok", res.Results);
            Assert.IsTrue(res.IsOK);
            Assert.IsFalse(res.IsScala);
        }

        [Test]
        public async Task TestIsScala()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Get, "http://test/api/55")
                .Respond(new Dictionary<string, string>()
                {
                    ["Api-Source"] = "blablabla-dfsc-blablabla"
                }, new StringContent("ok"));

            var client = new RrCompatibleHttpClient(
                new[] { "http://test/" },
                null,
                3,
                mockHttp);
            var res = await client.ExecuteAsync(HttpMethod.Get, "api/55", null, null);
            Assert.AreEqual("ok", res.Results);
            Assert.IsTrue(res.IsOK);
            Assert.IsTrue(res.IsScala);
        }

        [Test]
        public async Task TestError()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Get, "http://test/api/55")
                .Respond(HttpStatusCode.ServiceUnavailable, new StringContent("fail"));

            var client = new RrCompatibleHttpClient(
                new[] { "http://test/" },
                null,
                3,
                mockHttp);
            var res = await client.ExecuteAsync(HttpMethod.Get, "api/55", null, null);
            Assert.AreEqual("fail", res.Results);
            Assert.IsFalse(res.IsOK);
            Assert.IsFalse(res.IsScala);
            Assert.AreEqual(3, res.AttemptResults.Count);
            Assert.AreEqual(3, res.Exceptions.Count);
            Assert.AreEqual("http://test/api/55", res.Exceptions.Last().AbsoluteUri);
        }
    }
}
