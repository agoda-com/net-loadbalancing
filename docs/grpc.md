# Agoda.Frameworks.Grpc

## Overview
This is a gRPC client library that does automatic load-balancing using [Agoda.Frameworks.LoadBalancing](./load-balancing.md) under the hood.
It works by using DynamicProxy to intercept RPC method on generated client object,
then invoke the same method call on different a gRPC client targeting different server.

## Quick start
Suppose you have this sample proto file.
```proto
syntax = "proto3";

option csharp_namespace = "Example.Proto";

service SampleApi {
    rpc SampleRpcMethod (SampleRequest) returns (SampleResponse);
}

message SampleRequest {
    string payload = 1;
}

message SampleResponse {
    string payload = 1;
}
```

You will need to create a GrpcClientManager to get the load-balancing client.
The load-balancing client can be used as if you were using generated gPRC client object.
```c#
var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(
    new string[] { "server1", "server2" },
    maxRetry: 3);

var client = clientManager.GetClient(); // get actual client
client.SampleRpcMethod(...);            // might call server1 or server2
```

## Usage

### Simple
```c#
var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(new string[] { "server1" }, maxRetry: 3);
var client = clientManager.GetClient();
```

### Customized
This constructor allows you to set custom endpoint weight, weighing strategy, and retry predicate.
```c#
var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(
    new Dictionary<string, WeightItem>()
    {
        ["server1"] = new WeightItem(1000, 1000),
        ["server2"] = new WeightItem(3000, 3000)
    },
    new ExponentialWeightManipulationStrategy(10),
    (retryCount, exception) =>
    {
        // should retry logic
    });
var client = clientManager.GetClient();
```

### Updating resources

Updating resources should be done by using `GrpcClientManager.UpdateResources`.
If you already have the client, updating the GrpcClientManager will affect the client as well.
```c#
var clientManager = new GrpcClientManager<SampleApi.SampleApiClient>(new string[] { "server1", "server2" });
var client = clientManager.GetClient();
client.SampleRpcMethod(...); // might call server1 or server2

clientManager.UpdateResources(new string[] { "server1", "server3" });
client.SampleRpcMethod(...); // might call server1 or server3, but not server2
```

### Retry on failure
The client supports automatic retry for `Unavailable` and `Unknown` [status code](https://github.com/grpc/grpc/blob/master/doc/statuscodes.md) by default.
The number of retries can be set in the `GrpcClientManager` constructor.

Note that retrying `DeadlineExceeded` by using custom retry predicate will result in repeating `DeadlineExceeded` failure because the deadline is set once at the beginning of a method call.
In that case, you might want to re-invoke the method with different `deadline` value instead.
