# Using HttpClientFactory with Agoda.Frameworks.Http

## Overview

Agoda.Frameworks.Http is completely built on top HttpClient. By default, RandomUrlHttpClient instantiates its own HttpClient instance. However, it is highly recommended to use HttpClientFactory to supply HttpClient instance to RandomUrlHttpClient, as HttpClientFactory is designed to manage HttpClient's lifetime and avoid several pitfalls. Please check [Use HttpClientFactory to implement resilient HTTP requests](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) for further details.

## Usage

It is recommended to create typed client with HttpClientFactory and supply the HttpClient instance to RandomUrlHttpClient.

### Configuration

```
services.AddHttpClient<ITypedClient, TypedClient>();
```

### Typed Client

```
public interface ITypedClient
{
    Task<string> GetX();
}
public class TypedClient : ITypedClient
{
    public TypedClient(HttpClient httpClient)
    {
        HttpClient = new RandomUrlHttpClient(httpClient, new[]
        {
            "https://source1", "https://source2"
        }, isErrorResponse: (msg) =>
        {
            // customize error predicate
            // 0 for non-error
            return 0;
        });
    }

    public RandomUrlHttpClient HttpClient { get; }

    public async Task<string> GetX()
    {
        var response = await HttpClient.GetAsync("/x").ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync();
    }
}
```

## Samples

[Simple](../samples/HttpClientFactorySample)
[ASP.NET Core with Multiple Typed Clients](../samples/MultiTypedClient)
