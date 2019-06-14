using System;
namespace Agoda.Frameworks.Http
{
    public class ServiceUnavailableException : RouteException
    {
        public ServiceUnavailableException(
            string uri,
            string absoluteUri,
            string message,
            Exception innerException)
            : base(uri, absoluteUri, message, innerException)
        {
        }
    }
}
