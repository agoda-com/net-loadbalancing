using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;

namespace Agoda.Frameworks.Grpc
{

    /// <summary>
    /// Class to flatten Task<AsyncUnaryCall<TResponse>> to plain AsyncUnaryCall<TResponse>
    /// </summary>
    class AsyncUnaryCallWrapper<TResponse>
    {

        private Task<Tuple<TResponse, Metadata, Status, Metadata>> _result;

        public AsyncUnaryCallWrapper(Task<Tuple<TResponse, Metadata, Status, Metadata>> result)
        {
            _result = result;
        }

        public AsyncUnaryCall<TResponse> GetAsyncUnaryCall()
        {
            return new AsyncUnaryCall<TResponse>(
                GetResponseAsync(),
                GetResponseHeadersAsync(),
                GetStatus,
                GetTrailers,
                Cancel
            );
        }

        private async Task<TResponse> GetResponseAsync()
        {
            var resolved = await _result.ConfigureAwait(false);
            return resolved.Item1;
        }

        private async Task<Metadata> GetResponseHeadersAsync()
        {
            var resolved = await _result.ConfigureAwait(false);
            return resolved.Item2;
        }

        private Status GetStatus()
        {
            GrpcPreconditions.CheckState(_result.IsCompleted, "Status can only be accessed once the call has finished.");
            return _result.Result.Item3;
        }

        private Metadata GetTrailers()
        {
            GrpcPreconditions.CheckState(_result.IsCompleted, "Trailers can only be accessed once the call has finished.");
            return _result.Result.Item4;
        }

        private void Cancel()
        {
            // Cancellation is no-op for now
        }

    }
}
