using System;
using System.Net.Http;

namespace Agoda.Frameworks.Http
{
    public class HttpErrorResponseException : RouteResException
    {
        public int Code => (int)Response.StatusCode;

        public HttpErrorResponseException(
            string url,
            string combinedUrl,
            HttpResponseMessage res)
            : base(
                url,
                combinedUrl,
                $"Response status code does not indicate success: ${res.StatusCode}",
                res)
        {
        }
    }
}
