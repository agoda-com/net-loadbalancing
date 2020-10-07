using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Agoda.Frameworks.DB
{
    public interface IDbRepository
    {
        IEnumerable<TResult> Query<TRequest, TResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters);

        Task<IEnumerable<TResult>> QueryAsync<TRequest, TResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters);

        TResult QueryMultiple<TRequest, TResult>(
            IMultipleStoredProc<TRequest, TResult> sp,
            TRequest parameters);

        Task<TResult> QueryMultipleAsync<TRequest, TResult>(
            IMultipleStoredProc<TRequest, TResult> sp,
            TRequest parameters);

        /// <summary>
        /// Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        int ExecuteNonQuery<TRequest>(
            IStoredProc<TRequest> sp,
            TRequest parameters);

        /// <summary>
        /// Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        Task<int> ExecuteNonQueryAsync<TRequest>(
            IStoredProc<TRequest> sp,
            TRequest parameters);

        /// <summary>
        /// Execute stored procedure and builds a SqlDataReader.
        /// </summary>
        /// <param name="database">Database name. Should use the keys from IDbResources.</param>
        /// <param name="storedProc">Stored procedure name.</param>
        /// <param name="timeoutSecs">Command timeout value in seconds.</param>
        /// <param name="maxAttemptCount">Maximum attempt count including retries.</param>
        /// <param name="parameters">Parameters for SQL command.</param>
        /// <param name="callback">Callback function for generated SqlDataReader.</param>
        /// <remarks>Retry is not only applied to SQL connection, but also the invocation of callback.
        /// Do not put anything which is not related to SqlReader into callback.</remarks>
        T ExecuteReader<T>(
            string database,
            string storedProc,
            int timeoutSecs,
            int maxAttemptCount,
            IDbDataParameter[] parameters,
            Func<SqlDataReader, T> callback);

        /// <summary>
        /// Execute stored procedure and builds a SqlDataReader asynchronously.
        /// </summary>
        /// <param name="database">Database name. Should use the keys from IDbResources.</param>
        /// <param name="storedProc">Stored procedure name.</param>
        /// <param name="timeoutSecs">Command timeout value in seconds.</param>
        /// <param name="maxAttemptCount">Maximum attempt count including retries.</param>
        /// <param name="parameters">Parameters for SQL command.</param>
        /// <param name="callback">Callback function for generated SqlDataReader.</param>
        /// <remarks>Retry is not only applied to SQL connection, but also the invocation of callback.
        /// Do not put anything which is not related to SqlReader into callback.</remarks>
        Task<T> ExecuteReaderAsync<T>(
            string database,
            string storedProc,
            int timeoutSecs,
            int maxAttemptCount,
            IDbDataParameter[] parameters,
            Func<SqlDataReader, Task<T>> callback);

        event EventHandler<DbErrorEventArgs> OnError;
        event EventHandler<QueryCompleteEventArgs> OnQueryComplete;
        event EventHandler<ExecuteReaderCompleteEventArgs> OnExecuteReaderComplete;
    }
}