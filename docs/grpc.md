# Agoda.Frameworks.Grpc

## Overview
This is a gRPC client library that does automatic load-balancing using [Agoda.Frameworks.LoadBalancing](./load-balancing.md) under the hood.
It works by creating a custom gRPC CallInvoker which hold different channels to different servers.
CallInvoker will load-balance between channels on each gRPC client method call.

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

You will need to create a GrpcChannelManager to get the load-balancing call-invoker.
The load-balancing client can be used as if you were using generated gPRC client object.
```c#
var channelManager = new GrpcChannelManager(
    new string[] { "server1", "server2" },
    timeout: TimeSpan.FromMilliseconds(200),
    maxRetry: 3);

var lbCallInvoker = channelManager.GetCallInvoker();
var client = new SampleApi.SampleApiClient(lbCallInvoker);

client.SampleRpcMethod(...); // might call server1 or server2
```

## Usage

### Simple
```c#
var channelManager = new GrpcChannelManager(
    new string[] { "server1", "server2" },
    timeout: TimeSpan.FromMilliseconds(200),
    maxRetry: 3);

var lbCallInvoker = channelManager.GetCallInvoker();
var client = new SampleApi.SampleApiClient(lbCallInvoker);
```

### Customized
This constructor allows you to set custom endpoint weight, weighing strategy, timeout and retry predicate.
```c#
var channelManager = new GrpcChannelManager(
    new Dictionary<string, WeightItem>()
    {
        ["server1"] = new WeightItem(1000, 1000),
        ["server2"] = new WeightItem(3000, 3000)
    },
    new ExponentialWeightManipulationStrategy(10),
    timeout: TimeSpan.FromMilliseconds(200),
    (retryCount, exception) =>
    {
        // should retry logic
    });

var lbCallInvoker = channelManager.GetCallInvoker();
var client = new SampleApi.SampleApiClient(lbCallInvoker);
```

### Updating resources

Updating resources should be done by using `GrpcClientManager.UpdateResources`.
If you already have the client, updating the GrpcClientManager will affect the client as well.
```c#
var channelManager = new GrpcChannelManager(
    new string[] { "server1", "server2" },
    timeout: TimeSpan.FromMilliseconds(200));
var lbCallInvoker = clientManager.GetCallInvoker();
var client = new SampleApi.SampleApiClient(lbCallInvoker);

client.SampleRpcMethod(...); // might call server1 or server2

channelManager.UpdateResources(new string[] { "server1", "server3" });
client.SampleRpcMethod(...); // might call server1 or server3, but not server2
```

### Retry on failure
The client supports automatic retry for `DeadlineExceeded`, `Unavailable` and `Unknown` [status code](https://github.com/grpc/grpc/blob/master/doc/statuscodes.md) by default.
The number of retries can be set in the `GrpcClientManager` constructor.

### RPC Timeout
Note that `deadline` parameter in each gRPC call will be ignored and the timeout specified in `GrpcChannelManager` will be used instead.
But if timeout is set to null, then the load-balancing client will repect the timeout set by `deadline` parameter.
