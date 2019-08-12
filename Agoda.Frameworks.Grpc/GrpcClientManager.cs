using System;
using System.Collections.Generic;
using System.Linq;
using Agoda.Frameworks.LoadBalancing;
using Castle.DynamicProxy;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc
{
    public interface IGrpcClientManager<TClient> where TClient : ClientBase<TClient>
    {
        TClient GetClient();
        void UpdateResources(IReadOnlyDictionary<string, WeightItem> resources);
    }

    public class GrpcClientManager<TClient> : IGrpcClientManager<TClient> where TClient : ClientBase<TClient>
    {
        public IResourceManager<GrpcResource<TClient>> ResourceManager { get; }
        private readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();
        private readonly ShouldRetryPredicate _shouldRetry;

        public GrpcClientManager(string[] urls, int maxRetry = 1)
        {
            _shouldRetry = GetRetryCountPredicate(maxRetry);

            var resourceDict = CreateResourceDictionary(urls.ToDictionary(x => x, x => WeightItem.CreateDefaultItem()));
            ResourceManager = new ResourceManager<GrpcResource<TClient>>(resourceDict, new AgodaWeightManipulationStrategy());
        }

        public GrpcClientManager(
            IReadOnlyDictionary<string, WeightItem> resources,
            IWeightManipulationStrategy weightStrategy,
            ShouldRetryPredicate shouldRetry)
        {
            _shouldRetry = shouldRetry ?? throw new ArgumentNullException(nameof(shouldRetry));

            var resourceDict = CreateResourceDictionary(resources);
            ResourceManager = new ResourceManager<GrpcResource<TClient>>(resourceDict, weightStrategy);
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

        public TClient GetClient()
        {
            var lbInterceptor = new LoadBalancingInterceptor<TClient>(ResourceManager, _shouldRetry);
            var proxiedClient = _proxyGenerator.CreateClassProxy(typeof(TClient), lbInterceptor);
            return proxiedClient as TClient;
        }

        private IReadOnlyDictionary<GrpcResource<TClient>, WeightItem> CreateResourceDictionary(IReadOnlyDictionary<string, WeightItem> resources)
        {
            var newResourceDict = new Dictionary<GrpcResource<TClient>, WeightItem>();
            var currentResourceDict = ResourceManager?.Resources;
            var urlClientDict = currentResourceDict?.Keys.ToDictionary(x => x.Url, x => x);

            foreach (var i in resources)
            {
                var url = i.Key;
                GrpcResource<TClient> lookupKey;

                if (urlClientDict == null || !urlClientDict.ContainsKey(url))
                {
                    lookupKey = new GrpcResource<TClient>(url, CreateClient(url));
                }
                else
                {
                    lookupKey = urlClientDict[url];
                }
                newResourceDict.Add(lookupKey, i.Value);
            }

            return newResourceDict;
        }

        private TClient CreateClient(string url)
        {
            var channel = new Channel(url, ChannelCredentials.Insecure);
            var ctor = typeof(TClient).GetConstructor(new[] { typeof(Channel) });
            return ctor.Invoke(new[] { channel }) as TClient;
        }

        private static ShouldRetryPredicate GetRetryCountPredicate(int maxRetry) => (attemptCount, e) =>
        {
            if (e.InnerException is RpcException)
            {
                var statusCode = (e.InnerException as RpcException).StatusCode;

                if (statusCode == StatusCode.Unknown ||
                    statusCode == StatusCode.Unavailable)
                {
                    return attemptCount < maxRetry;
                }
            }
            return false;
        };
    }
}
