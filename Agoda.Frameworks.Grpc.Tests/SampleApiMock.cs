using System.Threading.Tasks;
using Agoda.Frameworks.Grpc.Tests.Proto;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc.Tests
{
    public class SampleApiMock : SampleApi.SampleApiBase
    {
        public delegate Task<SampleResponse> MockMethodHandler();

        private readonly MockMethodHandler _methodHandler;

        public SampleApiMock(MockMethodHandler methodHandler)
        {
            _methodHandler = methodHandler;
        }

        public static Server CreateLocalServer(MockMethodHandler methodHandler)
        {
            var server = new Server()
            {
                Services = { SampleApi.BindService(new SampleApiMock(methodHandler)) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            return server;
        }

        public override Task<SampleResponse> SampleRpcMethod(SampleRequest request, ServerCallContext context)
        {
            return _methodHandler();
        }
    }
}
