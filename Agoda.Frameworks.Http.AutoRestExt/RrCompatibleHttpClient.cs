using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Agoda.Frameworks.LoadBalancing;

namespace Agoda.Frameworks.Http.AutoRestExt
{
    public class RrCompatibleHttpClient
    {
        private readonly RandomUrlHttpClient _httpClient;
        private readonly bool _isGzip;

        public RrCompatibleHttpClient(
            string[] baseUrls,
            TimeSpan? timeout,
            int retryCount,
            HttpMessageHandler handler)
        {
            _isGzip = handler is HttpClientHandler httpHandler &&
                (httpHandler?.AutomaticDecompression ?? 0) > 0;
            _httpClient = new RandomUrlHttpClient(
                new HttpClient(handler, true),
                baseUrls,
                timeout ?? TimeSpan.FromMilliseconds(1000),
                retryCount);
        }

        public RrCompatibleHttpClient(
            string[] baseUrls,
            TimeSpan? timeout,
            int retryCount,
            bool isGzip,
            bool ignoreSslPolicyErrors = false)
            : this(baseUrls, timeout, retryCount, CreateDefaultHandler(isGzip, ignoreSslPolicyErrors))
        {
        }

        private static HttpClientHandler CreateDefaultHandler(bool isGzip, bool ignoreSslPolicyErrors)
        {
            var handler = new HttpClientHandler();
            if (isGzip)
            {
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            }
            if (ignoreSslPolicyErrors)
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) => true;
            }
            return handler;
        }

        public async Task<ExecuteResult> ExecuteAsync(
            HttpMethod verb,
            string url,
            string content,
            IReadOnlyDictionary<string, string> headers)
        {
            var results = await _httpClient.SendAsyncWithDiag(url, fullUrl =>
            {
                var msg = new HttpRequestMessage(verb, fullUrl);
                if (content != null)
                {
                    msg.Content = new StringContent(content);
                }
                if (headers != null)
                {
                    foreach (var pair in headers)
                    {
                        msg.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
                    }
                }
                return msg;
            });
            var prevResults = new List<ExecuteResult>();
            foreach (var msg in results)
            {
                prevResults.Add(await MsgToExecuteResult(msg, prevResults));
            }

            return prevResults.Last();
        }

        private async Task<ExecuteResult> MsgToExecuteResult(
            RetryActionResult<string, HttpResponseMessage> res,
            List<ExecuteResult> prevResults)
        {
            var response = GetResponse(res);
            var body = response != null
                ? await response.Content.ReadAsStringAsync()
                : "";
            var isScala = GetIsScala(response);
            var exceptions = (prevResults.LastOrDefault()?.Exceptions ?? new RouteException[0])
                .Concat(GetExceptionList(res))
                .ToList();
            return new ExecuteResult(
                body,
                (int)res.Elapsed.TotalMilliseconds,
                Encoding.UTF8.GetByteCount(body),
                _isGzip,
                res.Attempt - 1,
                GetUrl(res),
                GetStatusCode(response),
                isScala,
                !res.IsError,
                // Duplicate current list
                prevResults,
                exceptions);
        }

        private static IReadOnlyList<RouteException> GetExceptionList(
            RetryActionResult<string, HttpResponseMessage> res)
        {
            if (res.IsError && res.Exception is RouteException routeEx)
            {
                return new[] {routeEx};
            }

            return new RouteException[0];
        }

        private static int GetStatusCode(HttpResponseMessage response)
        {
            if (response != null)
            {
                return (int) response.StatusCode;
            }

            return 0;
        }

        private static string GetUrl(RetryActionResult<string, HttpResponseMessage> res)
        {
            var response = GetResponse(res);
            if (response != null)
            {
                return response.RequestMessage.RequestUri.ToString();
            }

            if (res.IsError && res.Exception is RouteException routeEx)
            {
                return routeEx.AbsoluteUri;
            }

            return null;
        }

        private static bool GetIsScala(HttpResponseMessage response)
        {
            if (response != null && response.Headers.TryGetValues("Api-Source", out var apiSources))
            {
                var apiSource = apiSources.FirstOrDefault();
                var isScala = !string.IsNullOrEmpty(apiSource) &&
                              apiSource.ToLower().Contains("dfsc");
                return isScala;
            }
            return false;
        }

        private static HttpResponseMessage GetResponse(RetryActionResult<string, HttpResponseMessage> res)
        {
            if (res.IsError && res.Exception is RouteResException routeResEx)
            {
                return routeResEx.Response;
            }
            return res.Result;
        }
    }
}
