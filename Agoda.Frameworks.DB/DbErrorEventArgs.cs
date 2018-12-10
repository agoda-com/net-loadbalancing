using System;

namespace Agoda.Frameworks.DB
{
    public sealed class DbErrorEventArgs : EventArgs
    {
        public DbErrorEventArgs(Exception error, int attemptCount)
        {
            Error = error;
            AttemptCount = attemptCount;
        }

        public Exception Error { get; }
        public int AttemptCount { get; }
    }
}
