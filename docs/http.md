# Agoda.Frameworks.Http

## Overview

Agoda.Frameworks.Http is an HTTP client library that features base URL randomization through [load-balancing](./load-balancing.md). It is built on top of HttpClient and handles several error handlings for developers:

- Timeout exception normalization
- Success status code is required by default
- Automatic retry for transient errors

### Timeout exception normalization

HttpClient throws TaskCanceledException instead of TimeoutException by design. And it can be annoying to distinguish timeout from other TaskCanceledException errors, or handle timeout errors with try-catch. RandomUrlHttpClient however, handles the timeout errors by default, and timeout errors will allways be TimeoutException when you set Timeout value to RandomUrlHttpClient.

### Success Status Code

[EnsureSuccessStatusCode](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.ensuresuccessstatuscode) is always called for every response.

### Automatic Retry for Transient Errors

Default error handling predicate only trigger retries on transient errors. Transient errors are internal server errors (status code > 500) or timeout error.

## Load-balancing

Agoda.Frameworks.Http has its own HttpClient which is RandomUrlHttpClient. RandomUrlHttpClient has the preset unchangeable weight manipulation strategy which adjusts the weight value for all base URL sources.

### Weight Range

1-1000

### Increment

Weight value is increased to 1000 when successful HTTP request is made.

### Decrement

Weight value is divided by 10 when HTTP request is failed.

### Reset and Update

Base URLs can be updated by method UpdateBaseUrls. All weight values for existing base URLs will be remained if the same base URL is presented on the new base URL list.

For further details, please check [load-balancing](./load-balancing.md).

## Usage

### Simple

```
var client = new RandomUrlHttpClient(
  // Base URLs
  new[] { "http://test1", "http://test2" });
// GET http://test1/api/x or http://test2/api/x
var res = await client.GetAsync("api/x");
```

### HttpClient Wrapper

```
var httpClient = new HttpClient(customHandler);
var client = new RandomUrlHttpClient(
  // Supply HttpClient to RandomUrlHttpClient
  httpClient,
  // Base URLs
  new[] { "http://test1", "http://test2" });
```

Note: Please check [HttpClientFactory](./http-httpclientfactory.md) for recommended usage in ASP.NET Core projects.

## Error Predicate

Some of the API servers send OK response to the client, but specify error message in the response body. In that case, you may want to treat that response as error response and initiate retry for RandomUrlHttpClient. To customize how RandomUrlHttpClient determines error response, you may add a predicate to RandomUrlHttpClient's constructor.

```
var client = new RandomUrlHttpClient(
  // Base URLs
  new[] { "http://test1", "http://test2" },
  isErrorResponse: (message, body) => {
    // message is HttpResponseMessage
    // body is response body from message.Content.ReadAsStringAsync()
    var resObj = JObject.Parse(body);
    // get errorCode from {"errorCode": 0}
    var errorCode = (int)resObj["errorCode"];
    // 0 for non-error, non-0 values are treated as errors
    return errorCode;
  });
```

## Determine Whether or Not to Retry

RandomUrlHttpClient retries on transient errors. Transient errors are internal server errors (status code > 500) or timeout error. In normal, read-only circumstances that should be good enough. However, for write requests such as payment API or booking API, you may want to bail-out and throw exception.

```
var client = new RandomUrlHttpClient(
  httpClient
  new[] { "http://test1", "http://test2" },
  timeout,
  isErrorResponse: null,
  shouldRetry: (error, attemptCount) => {
    // error is the exception that raised during the HTTP request
    // attemptCount is the current number of attempt
    // e.g. for first error, attemptCount is 1

    // false means no more retries
    return false;
  });
```

Notice that `shouldRetry` and `maxRetry` do not appear in the same constructor, as `maxRetry` is implemented through `shouldRetry` internally. If you want to customize your own retry predicate, you have to implement retry count checking by yourself.
