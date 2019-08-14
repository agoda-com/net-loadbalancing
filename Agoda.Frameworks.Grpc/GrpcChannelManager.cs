using System;
using System.Collections.Generic;
using System.Linq;
using Agoda.Frameworks.LoadBalancing;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc
{
    public interface IGrpcChannelManager
    {
        CallInvoker GetCallInvoker();
        void UpdateResources(IReadOnlyDictionary<string, WeightItem> resources);
        event EventHandler<GrpcErrorEventArgs> OnError;
    }

    public class GrpcChannelManager : IGrpcChannelManager
    {
        public IResourceManager<GrpcResource> ResourceManager { get; }
        public event EventHandler<GrpcErrorEventArgs> OnError;

        private readonly ShouldRetryPredicate _shouldRetry;
        private readonly TimeSpan? _timeout;

        public GrpcChannelManager(string[] urls, TimeSpan? timeout, int maxRetry = 1)
        {
            _shouldRetry = GetRetryCountPredicate(maxRetry);
            _timeout = timeout;

            var resourceDict = CreateResourceDictionary(urls.ToDictionary(x => x, x => WeightItem.CreateDefaultItem()));
            ResourceManager = new ResourceManager<GrpcResource>(resourceDict, new AgodaWeightManipulationStrategy());
        }

        public GrpcChannelManager(
            IReadOnlyDictionary<string, WeightItem> resources,
            IWeightManipulationStrategy weightStrategy,
            TimeSpan timeout,
            int maxRetry) : this(resources, weightStrategy, timeout, GetRetryCountPredicate(maxRetry))
        {
        }

        public GrpcChannelManager(
            IReadOnlyDictionary<string, WeightItem> resources,
            IWeightManipulationStrategy weightStrategy,
            TimeSpan timeout,
            ShouldRetryPredicate shouldRetry)
        {
            _shouldRetry = shouldRetry ?? throw new ArgumentNullException(nameof(shouldRetry));
            _timeout = timeout;

            var resourceDict = CreateResourceDictionary(resources);
            ResourceManager = new ResourceManager<GrpcResource>(resourceDict, weightStrategy);
        }

        public void UpdateResources(string[] urls)
        {
            var resources = urls.ToDictionary(x => x, x => WeightItem.CreateDefaultItem());
            UpdateResources(resources);
        }

        public void UpdateResources(IReadOnlyDictionary<string, WeightItem> resources)
        {
            var resourceDict = CreateResourceDictionary(resources);
            ResourceManager.UpdateResources(resourceDict);
        }

        public CallInvoker GetCallInvoker()
        {
            return new LoadBalancingCallInvoker(ResourceManager, _timeout, _shouldRetry, RaiseOnError);
        }

        private IReadOnlyDictionary<GrpcResource, WeightItem> CreateResourceDictionary(IReadOnlyDictionary<string, WeightItem> resources)
        {
            var newResourceDict = new Dictionary<GrpcResource, WeightItem>();
            var currentResourceDict = ResourceManager?.Resources;
            var urlClientDict = currentResourceDict?.Keys.ToDictionary(x => x.Url, x => x);

            foreach (var i in resources)
            {
                var url = i.Key;
                GrpcResource lookupKey;

                if (urlClientDict == null || !urlClientDict.ContainsKey(url))
                {
                    lookupKey = new GrpcResource(url, new Channel(url, ChannelCredentials.Insecure));
                }
                else
                {
                    lookupKey = urlClientDict[url];
                }
                newResourceDict.Add(lookupKey, i.Value);
            }

            return newResourceDict;
        }

        private static ShouldRetryPredicate GetRetryCountPredicate(int maxRetry) => (attemptCount, e) =>
        {
            if (e is RpcException rpcEx)
            {
                var statusCode = rpcEx.StatusCode;

                if (statusCode == StatusCode.Unknown ||
                    statusCode == StatusCode.Unavailable ||
                    statusCode == StatusCode.DeadlineExceeded)
                {
                    return attemptCount < maxRetry;
                }
            }
            return false;
        };

        protected virtual void RaiseOnError(Exception error, int attemptCount) =>
            OnError?.Invoke(this, new GrpcErrorEventArgs(error, attemptCount));
    }
}
