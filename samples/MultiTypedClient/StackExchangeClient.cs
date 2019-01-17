using System;
using System.Net.Http;
using System.Threading.Tasks;
using Agoda.Frameworks.Http;

namespace MultiTypedClient
{
    public interface IStackExchangeClient
    {
        Task<string> GetJson();
    }

    public class StackExchangeClient : IStackExchangeClient
    {
        public StackExchangeClient(HttpClient httpClient)
        {
            HttpClient = new RandomUrlHttpClient(httpClient, new[]
            {
                "https://api.stackexchange.com/2.2"
            }, isErrorResponse: (msg) =>
            {
                // customize error predicate
                // 0 for non-error
                return 0;
            });
        }

        public RandomUrlHttpClient HttpClient { get; }

        // Gets the list of sites on StackExchange.
        public async Task<string> GetJson()
        {
            var response = await HttpClient.GetAsync("/sites").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
