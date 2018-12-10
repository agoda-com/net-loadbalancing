using System;
using System.Threading.Tasks;
using Dapper;

namespace Agoda.Frameworks.DB
{
    public interface IStoredProc
    {
        string DbName { get; }
        string StoredProcedureName { get; }
        int CommandTimeoutSecs { get; }
        int MaxAttemptCount { get; }
    }

    public interface IStoredProc<TRequest> : IStoredProc
    {
    }

    // Not derived from IStoredProc<TRequst> by intention.
    // IStoredProc<TRequest, TResult> should not be used for ExecuteNonQuery
    public interface IStoredProc<in TRequest, TResult> : IStoredProc
    {
        TimeSpan? CacheLifetime { get; }
        SpParameter[] GetParameters(TRequest parameters);
    }

    public interface IMultipleStoredProc<in TRequest, TResult> : IStoredProc<TRequest, TResult>
    {
        TResult Read(SqlMapper.GridReader reader);
        // Consider to keep only ReadAsync, and use Wait for synchronous read.
        Task<TResult> ReadAsync(SqlMapper.GridReader reader);
    }
}
