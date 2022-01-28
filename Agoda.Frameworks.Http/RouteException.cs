using System;
using System.Net.Http;

namespace Agoda.Frameworks.Http
{
    public class RouteException : Exception
    {
        public string Uri { get; }
        public string AbsoluteUri { get; }

        public RouteException(string uri, string absoluteUri, string message)
            : this(uri, absoluteUri, message, null)
        {
        }

        public RouteException(string uri, string absoluteUri, string message, Exception innerException)
            : base(message, innerException)
        {
            Uri = uri;
            AbsoluteUri = absoluteUri;
        }
    }

    public class RouteResException : RouteException
    {
        public HttpResponseMessage Response { get; }

        public RouteResException(
            string uri,
            string absoluteUri,
            string message,
            HttpResponseMessage response) :
            base(uri, absoluteUri, message)
        {
            Response = response;
        }

        public RouteResException(
            string uri,
            string absoluteUri,
            string message,
            Exception innerException,HttpResponseMessage response) :
            base(uri, absoluteUri, message, innerException)
        {
            Response = response;
        }
    }
}
