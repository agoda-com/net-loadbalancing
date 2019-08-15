using System;
using System.Threading.Tasks;
using Agoda.Frameworks.LoadBalancing;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc
{
    class LoadBalancingCallInvoker : CallInvoker
    {
        private readonly IResourceManager<GrpcResource> _resourceManager;
        private readonly TimeSpan? _timeout;
        private readonly ShouldRetryPredicate _shouldRetry;
        private readonly OnError _onError;

        public LoadBalancingCallInvoker(
            IResourceManager<GrpcResource> resourceManager,
            TimeSpan? timeout,
            ShouldRetryPredicate shouldRetry,
            OnError onError)
        {
            _resourceManager = resourceManager;
            _timeout = timeout;
            _shouldRetry = shouldRetry;
            _onError = onError;
        }

        /// <summary>
        /// Invokes a simple remote call in a blocking fashion.
        /// </summary>
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return _resourceManager.ExecuteAction((grpcResource, retryCount) =>
            {
                var overriddenOptions = OverrideCallOptions(options);
                var call = new CallInvocationDetails<TRequest, TResponse>(grpcResource.Channel, method, host, overriddenOptions);
                return Calls.BlockingUnaryCall(call, request);
            }, _shouldRetry, _onError);
        }

        /// <summary>
        /// Invokes a simple remote call asynchronously.
        /// </summary>
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            var result = _resourceManager.ExecuteAsync(async (grpcResource, retryCount) =>
            {
                var overriddenOptions = OverrideCallOptions(options);
                var call = new CallInvocationDetails<TRequest, TResponse>(grpcResource.Channel, method, host, overriddenOptions);
                var asyncCall = Calls.AsyncUnaryCall(call, request);
                
                // Await for any exception, but return the original object because we don't want to lose any information
                var response = await asyncCall.ResponseAsync.ConfigureAwait(false);
                var headers = await asyncCall.ResponseHeadersAsync.ConfigureAwait(false);
                var status = asyncCall.GetStatus();
                var trailers = asyncCall.GetTrailers();

                return Tuple.Create(response, headers, status, trailers);
            }, _shouldRetry, _onError);

            return new AsyncUnaryCallWrapper<TResponse>(result).GetAsyncUnaryCall();
        }

        /// <summary>
        /// Invokes a server streaming call asynchronously.
        /// In server streaming scenario, client sends on request and server responds with a stream of responses.
        /// </summary>
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            throw new NotImplementedException("Streaming is not supported for load-balancing");
        }

        /// <summary>
        /// Invokes a client streaming call asynchronously.
        /// In client streaming scenario, client sends a stream of requests and server responds with a single response.
        /// </summary>
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotImplementedException("Streaming is not supported for load-balancing");
        }

        /// <summary>
        /// Invokes a duplex streaming call asynchronously.
        /// In duplex streaming scenario, client sends a stream of requests and server responds with a stream of responses.
        /// The response stream is completely independent and both side can be sending messages at the same time.
        /// </summary>
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotImplementedException("Streaming is not supported for load-balancing");
        }

        private CallOptions OverrideCallOptions(CallOptions options)
        {
            if (!_timeout.HasValue)
            {
                return options;
            }
            else
            {
                return options.WithDeadline(DateTime.UtcNow.AddMilliseconds(_timeout.Value.TotalMilliseconds));
            }
        }

    }
}
