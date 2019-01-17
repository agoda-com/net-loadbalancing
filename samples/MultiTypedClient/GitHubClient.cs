using System;
using System.Net.Http;
using System.Threading.Tasks;
using Agoda.Frameworks.Http;

namespace MultiTypedClient
{
    public interface IGitHubClient
    {
        Task<string> GetJson();
    }
    public class GitHubClient : IGitHubClient
    {
        public GitHubClient(HttpClient httpClient)
        {
            HttpClient = new RandomUrlHttpClient(httpClient, new[]
            {
                "https://github.agodadev.io/api/v3"
            }, isErrorResponse: (msg) =>
            {
                // customize error predicate
                // 0 for non-error
                return 0;
            });
        }

        public RandomUrlHttpClient HttpClient { get; }

        // Gets the list of organizations.
        public async Task<string> GetJson()
        {
            var response = await HttpClient.GetAsync("/organizations").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
