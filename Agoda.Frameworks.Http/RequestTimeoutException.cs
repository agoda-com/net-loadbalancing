using System;

namespace Agoda.Frameworks.Http
{
    public class RequestTimeoutException : RouteException
    {
        public RequestTimeoutException(
            string uri,
            string absoluteUri,
            string message,
            Exception innerException)
            : base(uri, absoluteUri, message, innerException)
        {
        }
    }
}
