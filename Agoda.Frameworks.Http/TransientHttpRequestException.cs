using System;
using System.Net;

namespace Agoda.Frameworks.Http
{
    public class TransientHttpRequestException : Exception
    {
        public TransientHttpRequestException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
