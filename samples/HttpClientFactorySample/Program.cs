using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Agoda.Frameworks.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HttpClientFactorySample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(b =>
            {
                b.AddFilter((category, level) => true); // Spam the world with logs.

                // Add console logger so we can see all the logging produced by the client by default.
                b.AddConsole(c => c.IncludeScopes = true);
            });

            Configure(serviceCollection);

            var services = serviceCollection.BuildServiceProvider();

            Console.WriteLine("Creating a client...");
            var stackExchange = services.GetRequiredService<StackExchangeClient>();

            Console.WriteLine("Sending a request...");
            var response = await stackExchange.GetJson();

            var data = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response data:");
            Console.WriteLine(data);

            Console.WriteLine("Press the ANY key to exit...");
            Console.ReadKey();
        }

        public static void Configure(IServiceCollection services)
        {
            services.AddHttpClient("stackexchange", c =>
            {
                c.DefaultRequestHeaders.Add("Accept", "application/json");
                c.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                // Compression is required by StackExchange
                // https://api.stackexchange.com/docs/compression
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddTypedClient<StackExchangeClient>();
        }

        private class StackExchangeClient
        {
            public StackExchangeClient(HttpClient httpClient)
            {
                HttpClient = new RandomUrlHttpClient(httpClient, new[]
                {
                    "https://api.stackexchange.com/2.2"
                });
            }

            public RandomUrlHttpClient HttpClient { get; }

            // Gets the list of sites on StackExchange.
            public async Task<HttpResponseMessage> GetJson()
            {
                var response = await HttpClient.GetAsync("/sites").ConfigureAwait(false);
                return response;
            }
        }
    }
}