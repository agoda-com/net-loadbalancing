using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agoda.RoundRobin;

namespace Benchmark
{
    public static class RR1
    {
        public static HttpClient CreateClient(string baseUrl)
        {
            var httpClient = new HttpClient(new HttpClientParameters
            {
                Name = "",
                ContentType = "application/json",
                //Settings =  new List<ServerSettings>()
                //{
                //    new ServerSettings("")
                //},
                Urls = new[] { baseUrl },
                Timeout = 100,
                Retry = 3
            });
            return httpClient;
        }
        public static async Task<object> Request(HttpClient client, string url)
        {
            return await client.ExecuteAsync(url, new byte[0], false, new ErrorList());
        }
    }
}
