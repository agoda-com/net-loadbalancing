using System;
using System.Collections.Generic;
using System.Text;

namespace Agoda.Frameworks.Http.AutoRestExt
{
    /// <summary>
    /// The execute builder class.
    /// </summary>
    public sealed class ExecuteResult
    {
        public string Results { get; }
        public int TotalExecMiliseconds { get; }
        public int TotalReceivedBytes { get; }
        public bool IsGzip { get; }
        public int RetryCount { get; }
        public string Uri { get; }
        public int Status { get; }
        public bool IsScala { get; }
        public bool IsOK { get; }
        public IReadOnlyList<ExecuteResult> AttemptResults { get; }
        public IReadOnlyList<RouteException> Exceptions { get; }

        //backing field for aggregate exception messaging
        private readonly Lazy<Exception> _aggregateException;

        public ExecuteResult(
            string results,
            int totalExecMiliseconds,
            int totalReceivedBytes,
            bool isGzip,
            int retryCount,
            string uri,
            int status,
            bool isScala,
            bool isOk,
            IReadOnlyList<ExecuteResult> attemptResults,
            IReadOnlyList<RouteException> exceptions)
        {
            Results = results;
            TotalExecMiliseconds = totalExecMiliseconds;
            TotalReceivedBytes = totalReceivedBytes;
            IsGzip = isGzip;
            RetryCount = retryCount;
            Uri = uri;
            Status = status;
            IsScala = isScala;
            IsOK = isOk;
            Exceptions = exceptions;
            AttemptResults = attemptResults;
            _aggregateException = new Lazy<Exception>(BuildAggregateException);
        }

        public Exception GetExeptions() => _aggregateException.Value;

        private Exception BuildAggregateException()
        {
            if (Exceptions.Count <= 0) return null;

            var sb = new StringBuilder("There are some exceptons occurred");
            foreach (var ex in Exceptions)
            {
                sb.Append(string.Format("\r\n - \"{0}\" -> {1}", ex.Uri, ex.Message));
            }

            return new Exception(sb.ToString());
        }
    }
}
