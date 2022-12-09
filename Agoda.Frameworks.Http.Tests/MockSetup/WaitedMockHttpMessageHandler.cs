using RichardSzalay.MockHttp;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Agoda.Frameworks.Http.Tests.MockSetup
{
    public class WaitedMockHttpMessageHandler : MockHttpMessageHandler
    {
        private readonly TimeSpan waitTime;

        public WaitedMockHttpMessageHandler(TimeSpan waitTime, BackendDefinitionBehavior backendDefinitionBehavior = BackendDefinitionBehavior.NoExpectations)
            : base(backendDefinitionBehavior)
        {
            this.waitTime = waitTime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var timeoutTask = Task.Delay(waitTime, cancellationToken);
            var respTask = base.SendAsync(request, cancellationToken);

            await Task.WhenAll(timeoutTask, respTask);

            return await respTask;
        }
    }
}
