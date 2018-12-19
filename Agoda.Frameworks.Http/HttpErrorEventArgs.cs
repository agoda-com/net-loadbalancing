using System;
namespace Agoda.Frameworks.Http
{
    public sealed class HttpErrorEventArgs : EventArgs
    {
        public HttpErrorEventArgs(Exception error, int attemptCount)
        {
            Error = error;
            AttemptCount = attemptCount;
        }

        public Exception Error { get; }
        public int AttemptCount { get; }
    }
}
