using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Agoda.Frameworks.LoadBalancing;
using Dapper;

namespace Agoda.Frameworks.DB
{
    public partial class DbRepository : IDbRepository
    {
        private readonly IDbResourceManager _dbResources;
        private readonly IDbCache _cache;
        private readonly Func<string, IDbConnection> _generateConnection;

        public event EventHandler<DbErrorEventArgs> OnError;
        public event EventHandler<QueryCompleteEventArgs> OnQueryComplete;
        public event EventHandler<ExecuteReaderCompleteEventArgs> OnExecuteReaderComplete;

        public DbRepository(
            IDbResourceManager dbResources,
            IDbCache cache,
            Func<string, IDbConnection> generateConnection)
        {
            _dbResources = dbResources;
            _cache = cache;
            _generateConnection = generateConnection;
            DefaultTimeoutSec = 5;
            DefaultMaxAttempts = 3;
        }

        public DbRepository(
            IDbResourceManager dbResources,
            IDbCache cache)
            : this(dbResources, cache, connStr => new SqlConnection(connStr))
        {
        }

        public DbRepository(
            IDbResourceManager dbResources)
            : this(dbResources, new DummyCache(), connStr => new SqlConnection(connStr))
        {
        }

        public int DefaultTimeoutSec { get; set; }

        public int DefaultMaxAttempts { get; set; }

        // TODO: Handle SqlException
        protected virtual ShouldRetryPredicate ShouldRetry(int maxAttemptCount)
            => (attemptcount, exception) => attemptcount < maxAttemptCount;

        private static bool EnableCache<TRequest, TResult>(IStoredProc<TRequest, TResult> sp)
        {
            return sp.CacheLifetime.HasValue && sp.CacheLifetime.Value > TimeSpan.Zero;
        }

        private TFuncResult ExecuteCacheOrGet<TRequest, TResult, TFuncResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters,
            Func<TFuncResult> getResultFunc,
            string cacheKey = "")
        {
            return EnableCache(sp)
                ? _cache.GetOrCreate(string.IsNullOrEmpty(cacheKey) ?
                    sp.GetParameters(parameters).CreateCacheKey(sp.StoredProcedureName) : cacheKey,
                    sp.CacheLifetime,
                    getResultFunc)
                : getResultFunc();
        }

        private Task<TFuncResult> ExecuteCacheOrGetAsync<TRequest, TResult, TFuncResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters,
            Func<Task<TFuncResult>> getResultFunc,
            string cacheKey = "")
        {
            return EnableCache(sp)
                ? _cache.GetOrCreateAsync(string.IsNullOrEmpty(cacheKey) ?
                    sp.GetParameters(parameters).CreateCacheKey(sp.StoredProcedureName): cacheKey,
                    sp.CacheLifetime,
                    getResultFunc)
                : getResultFunc();
        }

        public IEnumerable<TResult> Query<TRequest, TResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters,
            string cacheKey = "")
        {
            return ExecuteCacheOrGet(sp, parameters, () => QueryImpl(sp, parameters), cacheKey);
        }

        public Task<IEnumerable<TResult>> QueryAsync<TRequest, TResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters,
            string cacheKey = "")
        {
            return ExecuteCacheOrGetAsync(sp, parameters, () => QueryAsyncImpl(sp, parameters), cacheKey);
        }

        public TResult QueryMultiple<TRequest, TResult>(
            IMultipleStoredProc<TRequest, TResult> sp,
            TRequest parameters,
            string cacheKey = "")
        {
            return ExecuteCacheOrGet(sp, parameters, () => QueryMultipleImpl(sp, parameters), cacheKey);
        }

        public Task<TResult> QueryMultipleAsync<TRequest, TResult>(
            IMultipleStoredProc<TRequest, TResult> sp,
            TRequest parameters,
            string cacheKey = "")
        {
            return ExecuteCacheOrGetAsync(sp, parameters, () => QueryMultipleAsyncImpl(sp, parameters), cacheKey);
        }

        public int ExecuteNonQuery<TRequest>(
            IStoredProc<TRequest> sp,
            TRequest parameters)
        {
            var connectionStr = _dbResources.ChooseDb(sp.DbName).SelectRandomly();
            var stopwatch = Stopwatch.StartNew();
            Exception error = null;
            try
            {
                using (var connection = _generateConnection(connectionStr))
                {
                    return connection.Execute(
                        sp.StoredProcedureName,
                        parameters,
                        commandTimeout: sp.CommandTimeoutSecs,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception e)
            {
                RaiseOnError(e, 1);
                error = e;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                RaiseOnQueryComplete(sp, stopwatch.ElapsedMilliseconds, error);
            }
        }

        public async Task<int> ExecuteNonQueryAsync<TRequest>(
            IStoredProc<TRequest> sp,
            TRequest parameters)
        {
            // Copy-paste cannot be avoided due to ExecuteAsync must be completed inside of SqlConnection.
            var connectionStr = _dbResources.ChooseDb(sp.DbName).SelectRandomly();
            var stopwatch = Stopwatch.StartNew();
            Exception error = null;
            try
            {
                using (var connection = _generateConnection(connectionStr))
                {
                    return await connection.ExecuteAsync(
                        sp.StoredProcedureName,
                        parameters,
                        commandTimeout: sp.CommandTimeoutSecs,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception e)
            {
                RaiseOnError(e, 1);
                error = e;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                RaiseOnQueryComplete(sp, stopwatch.ElapsedMilliseconds, error);
            }
        }

        public T ExecuteReader<T>(
            string database,
            string storedProc,
            int timeoutSecs,
            int maxAttemptCount,
            IDbDataParameter[] parameters,
            Func<SqlDataReader, T> callback)
        {
            return _dbResources.ChooseDb(database).ExecuteAction((connectionStr, _) =>
            {
                var stopwatch = Stopwatch.StartNew();
                Exception error = null;
                try
                {
                    using (var connection = _generateConnection(connectionStr))
                    {
                        connection.Open();
                        SqlCommand sqlCommand = null;
                        try
                        {
                            sqlCommand = new SqlCommand(storedProc, connection as SqlConnection)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = timeoutSecs
                            };
                            sqlCommand.Parameters.AddRange(parameters);
                            using (var reader = sqlCommand.ExecuteReader())
                            {
                                return callback(reader);
                            }
                        }
                        finally
                        {
                            if (sqlCommand != null)
                            {
                                sqlCommand.Parameters.Clear();
                                sqlCommand.Dispose();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    RaiseOnExecuteReaderComplete(
                        database, storedProc, stopwatch.ElapsedMilliseconds, error);
                }
            }, ShouldRetry(maxAttemptCount), RaiseOnError);
        }

        public Task<T> ExecuteReaderAsync<T>(
            string database,
            string storedProc,
            int timeoutSecs,
            int maxAttemptCount,
            IDbDataParameter[] parameters,
            Func<SqlDataReader, Task<T>> callback)
        {
            return _dbResources.ChooseDb(database).ExecuteAsync(async (connectionStr, _) =>
            {
                var stopwatch = Stopwatch.StartNew();
                Exception error = null;
                try
                {
                    using (var connection = _generateConnection(connectionStr))
                    {
                        if (connection is SqlConnection sqlConn)
                        {
                            await sqlConn.OpenAsync();
                        }
                        else
                        {
                            connection.Open();
                        }
                        SqlCommand sqlCommand = null;
                        try
                        {
                            sqlCommand = new SqlCommand(storedProc, connection as SqlConnection)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = timeoutSecs
                            };
                            sqlCommand.Parameters.AddRange(parameters);
                            using (var reader = await sqlCommand.ExecuteReaderAsync())
                            {
                                return await callback(reader);
                            }
                        }
                        finally
                        {
                            if (sqlCommand != null)
                            {
                                sqlCommand.Parameters.Clear();
                                sqlCommand.Dispose();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    RaiseOnExecuteReaderComplete(
                        database, storedProc, stopwatch.ElapsedMilliseconds, error);
                }
            }, ShouldRetry(maxAttemptCount), RaiseOnError);
        }
        
        public Task<T> ExecuteReaderAsync<T>(
            string database,
            string storedProc,
            int timeoutSecs,
            int maxAttemptCount,
            int taskCancellationTimeOutInMilliSecs,
            IDbDataParameter[] parameters,
            Func<SqlDataReader, Task<T>> callback)
        {
            return _dbResources.ChooseDb(database).ExecuteAsync(async (connectionStr, _) =>
            {
                var stopwatch = Stopwatch.StartNew();
                Exception error = null;
                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(taskCancellationTimeOutInMilliSecs);
                try
                {
                    using (var connection = _generateConnection(connectionStr))
                    {
                        if (connection is SqlConnection sqlConn)
                        {
                            await sqlConn.OpenAsync();
                        }
                        else
                        {
                            connection.Open();
                        }
                        SqlCommand sqlCommand = null;
                        try
                        {
                            sqlCommand = new SqlCommand(storedProc, connection as SqlConnection)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = timeoutSecs
                            };
                            sqlCommand.Parameters.AddRange(parameters);
                            using (var reader = await sqlCommand.ExecuteReaderAsync(cancellationTokenSource.Token))
                            {
                                return await callback(reader);
                            }
                        }
                        finally
                        {
                            if (sqlCommand != null)
                            {
                                sqlCommand.Parameters.Clear();
                                sqlCommand.Dispose();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    RaiseOnExecuteReaderComplete(
                        database, storedProc, stopwatch.ElapsedMilliseconds, error);
                }
            }, ShouldRetry(maxAttemptCount), RaiseOnError);
        }

        private IEnumerable<TResult> QueryImpl<TRequest, TResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters)
        {
            return QuerySharedBlock(sp, connection =>
                connection.Query<TResult>(
                    sp.StoredProcedureName,
                    parameters,
                    commandTimeout: sp.CommandTimeoutSecs,
                    commandType: CommandType.StoredProcedure)
            );
        }

        private Task<IEnumerable<TResult>> QueryAsyncImpl<TRequest, TResult>(
            IStoredProc<TRequest, TResult> sp,
            TRequest parameters)
        {
            return QueryAsyncSharedBlock(sp, connection =>
                connection.QueryAsync<TResult>(
                    sp.StoredProcedureName,
                    parameters,
                    commandTimeout: sp.CommandTimeoutSecs,
                    commandType: CommandType.StoredProcedure)
            );
        }

        private TResult QueryMultipleImpl<TRequest, TResult>(
            IMultipleStoredProc<TRequest, TResult> sp,
            TRequest parameters)
        {
            return QuerySharedBlock(sp, connection =>
            {
                var reader = connection.QueryMultiple(
                    sp.StoredProcedureName,
                    parameters,
                    commandTimeout: sp.CommandTimeoutSecs,
                    commandType: CommandType.StoredProcedure);
                return sp.Read(reader);
            });
        }

        private Task<TResult> QueryMultipleAsyncImpl<TRequest, TResult>(
            IMultipleStoredProc<TRequest, TResult> sp,
            TRequest parameters)
        {
            return QueryAsyncSharedBlock(sp, async connection =>
            {
                var reader = await connection.QueryMultipleAsync(
                    sp.StoredProcedureName,
                    parameters,
                    commandTimeout: sp.CommandTimeoutSecs,
                    commandType: CommandType.StoredProcedure);
                return await sp.ReadAsync(reader);
            });
        }

        private TResult QuerySharedBlock<TResult>(
            IStoredProc sp,
            Func<IDbConnection, TResult> queryFunc)
        {
            return _dbResources.ChooseDb(sp.DbName).ExecuteAction((connectionStr, _) =>
            {
                var stopwatch = Stopwatch.StartNew();
                Exception error = null;
                try
                {
                    using (var connection = _generateConnection(connectionStr))
                    {
                        return queryFunc(connection);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    RaiseOnQueryComplete(sp, stopwatch.ElapsedMilliseconds, error);
                }
            }, ShouldRetry(sp.MaxAttemptCount), RaiseOnError);
        }

        private Task<TResult> QueryAsyncSharedBlock<TResult>(
            IStoredProc sp,
            Func<IDbConnection, Task<TResult>> queryFunc)
        {
            return _dbResources.ChooseDb(sp.DbName).ExecuteAsync(async (connectionStr, _) =>
            {
                var stopwatch = Stopwatch.StartNew();
                Exception error = null;
                try
                {
                    using (var connection = _generateConnection(connectionStr))
                    {
                        return await queryFunc(connection);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    RaiseOnQueryComplete(sp, stopwatch.ElapsedMilliseconds, error);
                }
            }, ShouldRetry(sp.MaxAttemptCount), RaiseOnError);
        }

        protected virtual void RaiseOnError(Exception error, int attemptCount) =>
            OnError?.Invoke(this, new DbErrorEventArgs(error, attemptCount));

        protected virtual void RaiseOnQueryComplete(IStoredProc sp, long time, Exception error) =>
            OnQueryComplete?.Invoke(this, new QueryCompleteEventArgs(sp, time, error));

        protected virtual void RaiseOnExecuteReaderComplete(
            string database,
            string storedProc,
            long executionTime,
            Exception error) =>
            OnExecuteReaderComplete?.Invoke(this,
                new ExecuteReaderCompleteEventArgs(database, storedProc, executionTime, error));
    }
}
