using System;
using Agoda.Frameworks.LoadBalancing;
using Castle.DynamicProxy;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc
{
    [Serializable]
    class LoadBalancingInterceptor<TClient> : IInterceptor where TClient : ClientBase<TClient>
    {
        private readonly ShouldRetryPredicate _shouldRetry;
        private readonly IResourceManager<GrpcResource<TClient>> _resourceManager;

        internal LoadBalancingInterceptor(
            IResourceManager<GrpcResource<TClient>> resourceManager,
            ShouldRetryPredicate shouldRetry)
        {
            _resourceManager = resourceManager;
            _shouldRetry = shouldRetry;
        }

        public void Intercept(IInvocation invocation)
        {
            var result = _resourceManager.ExecuteAction((grpcResource, retryCount) =>
            {
                var client = grpcResource.Client;
                var method = invocation.GetConcreteMethod();
                var parameters = invocation.Arguments;
                return method.Invoke(client, parameters);
            }, _shouldRetry);

            invocation.ReturnValue = result;
        }
    }
}
