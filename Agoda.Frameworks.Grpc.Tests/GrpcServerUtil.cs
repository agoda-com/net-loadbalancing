using System.Linq;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc.Tests
{
    public class GrpcServerUtil
    {
        public static string GetServerUrl(Server server)
        {
            var serverPort = server.Ports.First();
            var host = serverPort.Host;
            var port = serverPort.BoundPort;
            return $"{host}:{port}";
        }
    }
}
