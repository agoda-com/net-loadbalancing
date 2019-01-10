using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Agoda.Frameworks.Http;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace Agoda.Frameworks.LoadBalancing.Tests
{
    public class RandomUrlHttpClientTest
    {
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
    }
}
