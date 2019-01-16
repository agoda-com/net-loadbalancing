using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Agoda.Frameworks.Http;

namespace Benchmark
{
    class Program
    {
        static string baseUrl = "http://localhost:3000";
        static int testCount = 1000;
        static void Main(string[] args)
        {
            var native = new HttpClient();
            Console.WriteLine("Native");
            RunTest(testCount, async url =>
            {
                return await native.PostAsync(baseUrl, new StringContent(""));
            });

            var rr1 = RR1.CreateClient(baseUrl);
            Console.WriteLine("RR1");
            RunTest(testCount, url => RR1.Request(rr1, url));

            var newrrClient = new RandomUrlHttpClient(new[] { baseUrl });
            Console.WriteLine("RR3");
            RunTest(testCount, async url =>
            {
                return await newrrClient.PostAsync(url, new StringContent(""));
            });
        }

        private static void RunTest(int count, Func<string, Task<object>> get)
        {
            var requests = Enumerable.Range(0, count).Select(CreateRequest).ToList();
            var stopwatch = Stopwatch.StartNew();
            var requestTasks = requests.Select(r => MakeRequest(r, get)).ToArray();
            Task.WaitAll(requestTasks);
            stopwatch.Stop();

            Console.WriteLine("Sum: " + requests.Sum(x => x.DurationMs));
            Console.WriteLine("Avg: " + requests.Average(x => x.DurationMs));
            Console.WriteLine("Min: " + requests.Min(x => x.DurationMs));
            Console.WriteLine("Max: " + requests.Max(x => x.DurationMs));
        }

        private static async Task MakeRequest(Request request, Func<string, Task<object>> get)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await get(request.Url);
            stopwatch.Stop();

            // save request duration and response
            request.DurationMs = stopwatch.ElapsedMilliseconds;
            // request.ResponseId = ParseResponse(response);
        }

        private static Request CreateRequest(int i)
        {
            return new Request()
            {
                Url = "/"
            };
        }


    }
    public class Request
    {
        public string Url { get; set; }
        public long DurationMs { get; set; }
    }
}
