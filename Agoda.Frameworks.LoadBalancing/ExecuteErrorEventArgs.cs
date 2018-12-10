using System;

namespace Agoda.Frameworks.LoadBalancing
{
    public sealed class ExecuteErrorEventArgs : EventArgs
    {
        public ExecuteErrorEventArgs(Exception error, int attemptCount)
        {
            Error = error;
            AttemptCount = attemptCount;
        }

        public Exception Error { get; }
        public int AttemptCount { get; }
    }
}
