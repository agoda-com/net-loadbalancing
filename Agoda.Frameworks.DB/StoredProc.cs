using System;
using System.Linq;
using System.Reflection;
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

    public abstract class AutoStoredProc<TRequest, TResult> : IStoredProc<TRequest, TResult>
    {
        public abstract TimeSpan? CacheLifetime { get; }
        public abstract string DbName { get; }
        public abstract string StoredProcedureName { get; }
        public abstract int CommandTimeoutSecs { get; }
        public abstract int MaxAttemptCount { get; }
        public virtual SpParameter[] GetParameters(TRequest parameters)
        {
            var type = typeof(TRequest);
            return type.GetProperties()
                .OfType<PropertyInfo>()
                .Select(info => new SpParameter(info.Name, info.GetValue(parameters, null)))
                .ToArray();
        }
    }
}
