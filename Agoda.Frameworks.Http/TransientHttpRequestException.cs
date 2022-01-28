using System;
using System.Net;
using System.Net.Http;

namespace Agoda.Frameworks.Http
{
    public class TransientHttpRequestException : RouteResException
    {
        public TransientHttpRequestException(
            string uri,
            string absoluteUri,
            HttpResponseMessage res,
            string message)
            : base(uri, absoluteUri, message, res)
        {
        }

        public HttpStatusCode StatusCode => Response.StatusCode;
    }
}
