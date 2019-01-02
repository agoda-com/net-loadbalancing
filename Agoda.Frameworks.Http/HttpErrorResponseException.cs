using System;
namespace Agoda.Frameworks.Http
{
    public class HttpErrorResponseException : Exception
    {
        public HttpErrorResponseException(int code)
        {
            Code = code;
        }

        public int Code { get; }
    }
}
