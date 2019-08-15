using System;
using System.Collections.Generic;
using System.Text;

namespace Agoda.Frameworks.Grpc
{
    public sealed class GrpcErrorEventArgs : EventArgs
    {
        public GrpcErrorEventArgs(Exception error, int attemptCount)
        {
            Error = error;
            AttemptCount = attemptCount;
        }

        public Exception Error { get; }
        public int AttemptCount { get; }
    }
}
