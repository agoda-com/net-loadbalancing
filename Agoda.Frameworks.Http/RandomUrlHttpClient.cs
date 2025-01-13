﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
            if (e is TimeoutException ||
                e is TransientHttpRequestException ||
                e is ServiceUnavailableException)
            {
                return attemptCount < maxRetry;
            }
            return false;
        };

        public void UpdateBaseUrls(string[] baseUrls)
        {
            var dict = baseUrls
                .Distinct()
                .ToDictionary(x => x, _ => WeightItem.CreateDefaultItem());
            UrlResourceManager.UpdateResources(dict);
        }

        private static IResourceManager<string> CreateResourceManager(string[] baseUrls)
        {
            var dict = baseUrls
                .Distinct()
                .ToDictionary(x => x, x => WeightItem.CreateDefaultItem());
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

        public Task<HttpResponseMessage> GetAsync(string url) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.GetAsync(uri, cxlToken));
        public Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.SendAsync(AddHeaders(new HttpRequestMessage(HttpMethod.Get, uri), headers), cxlToken));

#if !NET462
        public Task<HttpResponseMessage> PostAsync(string url, HttpContent content) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.PostAsync(uri, content, cxlToken));

        public Task<HttpResponseMessage> PutAsync(string url, HttpContent content) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.PutAsync(uri, content, cxlToken));
#endif
        public Task<HttpResponseMessage> PostJsonAsync(string url, string json) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"), cxlToken));
        
        public Task<HttpResponseMessage> PostJsonAsync(string url, string json, Dictionary<string, string> headers) =>
            SendAsync(url, (uri, cxlToken) =>
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return HttpClient.SendAsync(AddHeaders(requestMessage, headers), cxlToken);
            });


        public Task<HttpResponseMessage> PutJsonAsync(string url, string json) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.PutAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"), cxlToken));
        
        public Task<HttpResponseMessage> PutJsonAsync(string url, string json, Dictionary<string, string> headers) =>
            SendAsync(url, (uri, cxlToken) =>
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return HttpClient.SendAsync(AddHeaders(requestMessage, headers), cxlToken);
            });

        public Task<HttpResponseMessage> DeleteAsync(string url) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.DeleteAsync(uri, cxlToken));
        
        public Task<HttpResponseMessage> DeleteAsync(string url, Dictionary<string, string> headers) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.SendAsync(AddHeaders(new HttpRequestMessage(HttpMethod.Delete, uri), headers), cxlToken));

        private HttpRequestMessage AddHeaders(HttpRequestMessage requestMessage, Dictionary<string, string> headers = null)
        {
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Key, header.Value);
            }
            return requestMessage;
        }

        public Task<HttpResponseMessage> SendAsync(
            string url,
            Func<string, HttpRequestMessage> requestMsg,
            Dictionary<string, string> headers) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.SendAsync(AddHeaders(requestMsg(uri), headers), cxlToken));


        public Task<IReadOnlyList<RetryActionResult<string, HttpResponseMessage>>> SendAsyncWithDiag(
            string url,
            Func<string, HttpRequestMessage> requestMsg,
            Dictionary<string, string> headers) =>
            SendAsyncWithDiag(url, (uri, cxlToken) => HttpClient.SendAsync(AddHeaders(requestMsg(uri), headers), cxlToken));

        public Task<HttpResponseMessage> SendAsync(
            string url,
            Func<string, HttpRequestMessage> requestMsg,
            Dictionary<string, string> headers,
            bool isThrow) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.SendAsync(AddHeaders(requestMsg(uri), headers), cxlToken), isThrow);


        public Task<IReadOnlyList<RetryActionResult<string, HttpResponseMessage>>> SendAsyncWithDiag(
            string url,
            Func<string, HttpRequestMessage> requestMsg,
            Dictionary<string, string> headers,
            bool isThrow) =>
            SendAsyncWithDiag(url, (uri, cxlToken) => HttpClient.SendAsync(AddHeaders(requestMsg(uri), headers), cxlToken), isThrow);

        public Task<HttpResponseMessage> SendAsync(
            string url,
            Func<string, HttpRequestMessage> requestMsg) =>
            SendAsync(url, (uri, cxlToken) => HttpClient.SendAsync(requestMsg(uri), cxlToken));


        public Task<IReadOnlyList<RetryActionResult<string, HttpResponseMessage>>> SendAsyncWithDiag(
            string url,
            Func<string, HttpRequestMessage> requestMsg) =>
            SendAsyncWithDiag(url, (uri, cxlToken) => HttpClient.SendAsync(requestMsg(uri), cxlToken));

        private async Task<HttpResponseMessage> SendAsync(
            string url,
            Func<string, CancellationToken, Task<HttpResponseMessage>> send,
            bool isThrow = true)
        {
            var results = await SendAsyncWithDiag(url, send, isThrow);
            var result = results.Last();
            if (isThrow && result.IsError)
            {
                throw result.Exception;
            }
            return result.Result;
        }

        private Task<IReadOnlyList<RetryActionResult<string, HttpResponseMessage>>> SendAsyncWithDiag(
           string url,
           Func<string, CancellationToken, Task<HttpResponseMessage>> send,
           bool isThrow = true)
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
                        var res = await send(combinedUrl, cts.Token);
                        if (!isThrow) return res;
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
                    catch (HttpRequestException e)
                    {
                        throw new ServiceUnavailableException(url, combinedUrl, e.Message, e);
                    }
                    catch (TaskCanceledException e)
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
