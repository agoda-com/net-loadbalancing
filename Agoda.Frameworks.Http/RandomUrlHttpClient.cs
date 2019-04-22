using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Agoda.Frameworks.LoadBalancing;

namespace Agoda.Frameworks.Http
{
    public class RandomUrlHttpClient : IDisposable
    {
        public HttpClient HttpClient { get; }
        public IResourceManager<string> UrlResourceManager { get; }
        public TimeSpan? Timeout { get; }
        private readonly Func<HttpResponseMessage, string, int> _isErrorResponse = (_1, _2) => 0;
        private readonly ShouldRetryPredicate _shouldRetry;

        public event EventHandler<HttpErrorEventArgs> OnError;

        public RandomUrlHttpClient(
            string[] baseUrls,
            TimeSpan? timeout = null,
            int maxRetry = 3,
            Func<HttpResponseMessage, string, int> isErrorResponse = null)
            : this(new HttpClient(), baseUrls, timeout, maxRetry, isErrorResponse)
        {
        }

        public RandomUrlHttpClient(
            HttpClient httpClient,
            string[] baseUrls,
            TimeSpan? timeout = null,
            int maxRetry = 3,
            Func<HttpResponseMessage, string, int> isErrorResponse = null)
            : this(httpClient, baseUrls, timeout, isErrorResponse, GetRetryCountPredicate(maxRetry))
        {
        }

        public RandomUrlHttpClient(
            HttpClient httpClient,
            string[] baseUrls,
            TimeSpan? timeout,
            Func<HttpResponseMessage, string, int> isErrorResponse,
            ShouldRetryPredicate shouldRetry)
        {
            HttpClient = httpClient;
            UrlResourceManager = CreateResourceManager(baseUrls);
            Timeout = timeout;
            if (isErrorResponse != null)
            {
                _isErrorResponse = isErrorResponse;
            }
            _shouldRetry = shouldRetry ?? throw new ArgumentNullException(nameof(shouldRetry));
        }

        private static ShouldRetryPredicate GetRetryCountPredicate(int maxRetry) => (attemptCount, e) =>
        {
            if (e is TimeoutException || e is TransientHttpRequestException)
            {
                return attemptCount < maxRetry;
            }
            return false;
        };

        public void UpdateBaseUrls(string[] baseUrls)
        {
            var dict = baseUrls.ToDictionary(x => x, x => WeightItem.CreateDefaultItem());
            UrlResourceManager.UpdateResources(dict);
        }

        private static IResourceManager<string> CreateResourceManager(string[] baseUrls)
        {
            var dict = baseUrls.ToDictionary(x => x, x => WeightItem.CreateDefaultItem());
            var mgr = new ResourceManager<string>(dict, new AgodaWeightManipulationStrategy());
            return mgr;
        }

        public void Dispose()
        {
            if (HttpClient != null)
            {
                HttpClient.Dispose();
            }
        }

        public Task<HttpResponseMessage> PostAsync(string url, HttpContent content) =>
            SendAsync(url, uri => HttpClient.PostAsync(uri, content));

        public Task<HttpResponseMessage> GetAsync(string url) =>
            SendAsync(url, uri => HttpClient.GetAsync(uri));

        public Task<HttpResponseMessage> PutAsync(string url, HttpContent content) =>
            SendAsync(url, uri => HttpClient.PutAsync(uri, content));

        public Task<HttpResponseMessage> DeleteAsync(string url) =>
            SendAsync(url, uri => HttpClient.DeleteAsync(uri));

        public Task<HttpResponseMessage> SendAsync(
            string url,
            Func<string, HttpRequestMessage> requestMsg) =>
            SendAsync(url, uri => HttpClient.SendAsync(requestMsg(url)));

        public Task<IReadOnlyList<RetryActionResult<string, HttpResponseMessage>>> SendAsyncWithDiag(
            string url,
            Func<string, HttpRequestMessage> requestMsg) =>
            SendAsyncWithDiag(url, uri => HttpClient.SendAsync(requestMsg(url)));

        private async Task<HttpResponseMessage> SendAsync(
            string url,
            Func<string, Task<HttpResponseMessage>> send)
        {
            var results = await SendAsyncWithDiag(url, send);
            var result = results.Last();
            if (result.IsError)
            {
                throw result.Exception;
            }
            return result.Result;
        }

        private Task<IReadOnlyList<RetryActionResult<string, HttpResponseMessage>>> SendAsyncWithDiag(
            string url,
            Func<string, Task<HttpResponseMessage>> send)
        {
            return UrlResourceManager.ExecuteAsyncWithDiag(async (source, _) =>
            {
                var combinedUrl = $"{source.TrimEnd('/')}/{url.TrimStart('/')}";
                // Special timeout handling for HttpClient
                using (var cts = new CancellationTokenSource())
                {
                    if (Timeout.HasValue)
                    {
                        cts.CancelAfter(Timeout.Value);
                    }
                    try
                    {
                        var res = await send(combinedUrl);
                        if (IsTransientHttpStatusCode(res.StatusCode))
                        {
                            throw new TransientHttpRequestException(
                                url,
                                combinedUrl, 
                                res,
                                $"Response status code does not indicate success: ${res.StatusCode}");
                        }
                        if (!res.IsSuccessStatusCode)
                        {
                            throw new HttpErrorResponseException(
                                url,
                                combinedUrl,
                                res);
                        }
                        var errorCode = _isErrorResponse(res, await res.Content.ReadAsStringAsync());
                        if (errorCode > 0)
                        {
                            throw new HttpErrorResponseException(
                                url,
                                combinedUrl,
                                res);
                        }
                        return res;
                    }
                    catch (TaskCanceledException e)
                        when (!cts.Token.IsCancellationRequested)
                    {
                        throw new RequestTimeoutException(url, combinedUrl, "Operation timeout", e);
                    }
                }
            }, _shouldRetry, RaiseOnError);
        }

        private static bool IsTransientHttpStatusCode(HttpStatusCode code)
        {
            return (int)code >= 500 || code == HttpStatusCode.RequestTimeout;
        }

        protected virtual void RaiseOnError(Exception error, int attemptCount) =>
            OnError?.Invoke(this, new HttpErrorEventArgs(error, attemptCount));
    }
}
