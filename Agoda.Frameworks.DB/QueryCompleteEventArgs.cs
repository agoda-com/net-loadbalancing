using System;

namespace Agoda.Frameworks.DB
{
    public sealed class QueryCompleteEventArgs : EventArgs
    {
        public QueryCompleteEventArgs(IStoredProc storedProc, long executionTime, Exception error)
        {
            StoredProc = storedProc;
            ExecutionTime = executionTime;
            Error = error;
        }

        public IStoredProc StoredProc { get; }
        public long ExecutionTime { get; }
        public Exception Error { get; }
    }
}