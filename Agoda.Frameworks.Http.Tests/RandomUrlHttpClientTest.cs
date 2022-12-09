using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Agoda.Frameworks.Http;
using Agoda.Frameworks.Http.Tests.MockSetup;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace Agoda.Frameworks.LoadBalancing.Tests
{
    public class RandomUrlHttpClientTest
    {
        [Test]
        public void CtorDuplicatedUrl()
        {
            var client = new RandomUrlHttpClient(
                new[] { "http://test/1", "http://test/1" });
            Assert.AreEqual(1, client.UrlResourceManager.Resources.Count);
            Assert.AreEqual("http://test/1", client.UrlResourceManager.Resources.Keys.First());
        }

        [Test]
        public async Task TestGetAsync()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Get, "http://test/*")
                .Respond(msg =>
                {
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    return new StringContent("ok");
                });
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                null);
            var res = await client.GetAsync("api/55");
            var content = await res.Content.ReadAsStringAsync();

            Assert.AreEqual("ok", content);
        }

        [Test]
        public async Task TestPostAsync()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Post, "http://test/*")
                .Respond(async (msg) =>
                {
                    Assert.AreEqual("55", await msg.Content.ReadAsStringAsync());
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    var resmsg = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("ok")
                    };
                    return resmsg;
                });
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                null);
            var res = await client.PostAsync("api/55", new StringContent("55"));
            var content = await res.Content.ReadAsStringAsync();

            Assert.AreEqual("ok", content);
        }

        [Test]
        public async Task TestPostJsonAsync()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Post, "http://test/*")
                .With(arg =>
                {
                    // Must provide content-type
                    return arg.Content.Headers.ContentType.MediaType == "application/json";
                })
                .Respond(async (msg) =>
                {
                    Assert.AreEqual("{\"foo\":\"bar\"}", await msg.Content.ReadAsStringAsync());
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    var resmsg = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("ok")
                    };
                    return resmsg;
                });
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                null);
            var res = await client.PostJsonAsync("api/55", "{\"foo\":\"bar\"}");
            var content = await res.Content.ReadAsStringAsync();

            Assert.AreEqual("ok", content);
        }

        [Test]
        public void TestRetry()
        {
            var mockHttp = new MockHttpMessageHandler();

            var counter = 0;
            mockHttp.When(HttpMethod.Post, "http://test/*")
                .Respond(msg =>
                {
                    counter++;
                    Assert.LessOrEqual(counter, 3);
                    return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
                });
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                null);

            Assert.ThrowsAsync<TransientHttpRequestException>(
                () => client.PostAsync("api/55", new StringContent("55")));
            Assert.AreEqual(3, counter);
        }

        [Test]
        public async Task TestHttpRequestException()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Post, "http://test/*")
                .Throw(new HttpRequestException());
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                null);

            var result = await client.SendAsyncWithDiag("api/55", url => new HttpRequestMessage(HttpMethod.Post, url));

            Assert.IsAssignableFrom<ServiceUnavailableException>(result.Last().Exception);
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void UpdateBaseUrls()
        {
            var client = new RandomUrlHttpClient(new[] { "http://test/1", "http://test/2" });

            // Should not throw due to identical items in the list (Distinct before ToDictionary)
            client.UpdateBaseUrls(new[] { "http://test/3", "http://test/3" });
            Assert.AreEqual("http://test/3", client.UrlResourceManager.SelectRandomly());
        }

        [Test]
        public void ShouldThrowRequestTimeoutExceptionWhenHttpRequestTimesOut()
        {
            var mockHttp = new WaitedMockHttpMessageHandler(new TimeSpan(0, 0, 3));

            mockHttp.When(HttpMethod.Get, "http://test/*")
                .Respond(msg =>
                {
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    return new StringContent("ok");
                });
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                new TimeSpan(0, 0, 1),
                3,
                null);

            Assert.ThrowsAsync<RequestTimeoutException>(() => client.GetAsync("api/55"));
        }

        [Test]
        public async Task ShouldNotThrowRequestTimeoutExceptionWhenHttpRequestDoesNotTimesOut()
        {
            var mockHttp = new WaitedMockHttpMessageHandler(new TimeSpan(0, 0, 1));

            mockHttp.When(HttpMethod.Get, "http://test/*")
                .Respond(msg =>
                {
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    return new StringContent("ok");
                });
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                new TimeSpan(0, 0, 3),
                3,
                null);

            var res = await client.GetAsync("api/55");
            var content = await res.Content.ReadAsStringAsync();

            Assert.AreEqual("ok", content);
        }

        [Test]
        public async Task ShouldNotThrowRequestTimeoutExceptionWhenNoTimesOut()
        {
            var mockHttp = new WaitedMockHttpMessageHandler(new TimeSpan(0, 0, 1));

            mockHttp.When(HttpMethod.Get, "http://test/*")
                .Respond(msg =>
                {
                    StringAssert.IsMatch("http://test/1|2/api/55", msg.RequestUri.AbsoluteUri);
                    return new StringContent("ok");
                });
            var httpclient = mockHttp.ToHttpClient();

            var client = new RandomUrlHttpClient(
                httpclient,
                new[] { "http://test/1", "http://test/2" },
                null,
                3,
                null);

            var res = await client.GetAsync("api/55");
            var content = await res.Content.ReadAsStringAsync();

            Assert.AreEqual("ok", content);
        }
    }
}
