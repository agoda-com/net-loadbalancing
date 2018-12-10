using System;

namespace Agoda.Frameworks.DB
{
    public sealed class ExecuteReaderCompleteEventArgs : EventArgs
    {
        public string Database { get; }
        public string StoredProc { get; }
        public long ExecutionTime { get; }
        public Exception Error { get; }

        public ExecuteReaderCompleteEventArgs(
            string database,
            string storedProc,
            long executionTime,
            Exception error)
        {
            Database = database;
            StoredProc = storedProc;
            ExecutionTime = executionTime;
            Error = error;
        }
    }
}