using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;


namespace Agoda.Frameworks.DB
{
    public partial class DbRepository // new simplified stuff
    {
        public T ExecuteReader<T>(string database, string storedProc, int timeoutSecs, int maxAttemptCount,
            IDbDataParameter[] parameters, Func<SqlDataReader, T> callback, TimeSpan? timeSpan)
        {
            return ExecuteCacheOrGet(storedProc, parameters,
                () => ExecuteReader(database, storedProc, timeoutSecs, maxAttemptCount, parameters, callback),
                timeSpan);
        }

        public Task<T> ExecuteReaderAsync<T>(string database, string storedProc, int timeoutSecs, int maxAttemptCount,
            IDbDataParameter[] parameters, Func<SqlDataReader, Task<T>> callback, TimeSpan? timeSpan,string cacheKey = "")
        {
            return ExecuteCacheOrGetAsync(storedProc, parameters,
                () => ExecuteReaderAsync(database, storedProc, timeoutSecs, maxAttemptCount, parameters, callback),
                timeSpan, cacheKey);
        }

        public async Task<object> ExecuteScalarAsync(
            string dbName,
            string sqlCommandString,
            CommandType commandType,
            object parameters)
        {
            // Copy-paste cannot be avoided due to ExecuteAsync must be completed inside of SqlConnection.
            var connectionStr = _dbResources.ChooseDb(dbName).SelectRandomly();
            var stopwatch = Stopwatch.StartNew();
            Exception error = null;
            try
            {
                using (var connection = _generateConnection(connectionStr))
                {
                    return await connection.ExecuteScalarAsync(
                        sqlCommandString,
                        parameters,
                        commandTimeout: DefaultTimeoutSec,
                        commandType: commandType);
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
                RaiseOnQueryComplete(new IAmNotAStoredProc(dbName, sqlCommandString, DefaultTimeoutSec, DefaultMaxAttempts), stopwatch.ElapsedMilliseconds, error);
            }
        }

        public Task<IEnumerable<T>> ExecuteQueryAsync<T>(
            string dbName,
            string sqlCommandString,
            CommandType commandType,
            object parameters,
            TimeSpan? timeSpan,
            string cacheKey = "")
        {
            return ExecuteCacheOrGetAsync(sqlCommandString, parameters,
                    () => ExecuteQueryAsync<T>(dbName, sqlCommandString, commandType, parameters), 
                    timeSpan, cacheKey);
        }

        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(
            string dbName,
            string sqlCommandString,
            CommandType commandType,
            object parameters)
        {
            // Copy-paste cannot be avoided due to ExecuteAsync must be completed inside of SqlConnection.
            var connectionStr = _dbResources.ChooseDb(dbName).SelectRandomly();
            var stopwatch = Stopwatch.StartNew();
            Exception error = null;
            try
            {
                using (var connection = _generateConnection(connectionStr))
                {
                    return await connection.QueryAsync<T>(
                        sqlCommandString,
                        parameters,
                        commandTimeout: DefaultTimeoutSec,
                        commandType: commandType);
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
                RaiseOnQueryComplete(new IAmNotAStoredProc(dbName, sqlCommandString, DefaultTimeoutSec, DefaultMaxAttempts), stopwatch.ElapsedMilliseconds, error);
            }
        }

        public Task<T> ExecuteQuerySingleAsync<T>(
            string dbName,
            string sqlCommandString,
            CommandType commandType,
            object parameters,
            TimeSpan? timeSpan,
            string cacheKey = "")
        {
            return ExecuteCacheOrGetAsync(sqlCommandString, parameters,
                () => ExecuteQuerySingleAsync<T>(dbName, sqlCommandString, commandType, parameters), timeSpan, cacheKey);
        }
        
        public async Task<T> ExecuteQuerySingleAsync<T>(
            string dbName,
            string sqlCommandString,
            CommandType commandType,
            object parameters)
        {
            // Copy-paste cannot be avoided due to ExecuteAsync must be completed inside of SqlConnection.
            var connectionStr = _dbResources.ChooseDb(dbName).SelectRandomly();
            var stopwatch = Stopwatch.StartNew();
            Exception error = null;
            try
            {
                using (var connection = _generateConnection(connectionStr))
                {
                    return await connection.QuerySingleAsync<T>(
                        sqlCommandString,
                        parameters,
                        commandTimeout: DefaultTimeoutSec,
                        commandType: commandType);
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
                RaiseOnQueryComplete(new IAmNotAStoredProc(dbName, sqlCommandString, DefaultTimeoutSec, DefaultMaxAttempts), stopwatch.ElapsedMilliseconds, error);
            }
        }
        private static string CreateCacheKey(string sqlCommandString, object parameters)
        {
            var sb = new StringBuilder();
            sb.Append(sqlCommandString);
            sb.Append(":");
            foreach(var p in parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                sb.Append($"@{p.Name}+{p.GetValue(parameters)}&");
            }

            return sb.ToString();
        }
        private static string CreateCacheKey(string sqlCommandString, IDbDataParameter[] parameters)
        {
            var sb = new StringBuilder();
            sb.Append(sqlCommandString);
            sb.Append(":");
            foreach(var p in parameters)
            {
                sb.Append($"{p.ParameterName}+{p.Value}&");
            }

            return sb.ToString();
        }
        private static bool EnableCache(TimeSpan? timeSpan)
        {
            return timeSpan.HasValue && timeSpan.Value > TimeSpan.Zero;
        }
        private Task<TFuncResult> ExecuteCacheOrGetAsync<TFuncResult>(
            string sqlCommandString,
            object parameters,
            Func<Task<TFuncResult>> getResultFunc,
            TimeSpan? timeSpan,
            string cacheKey ="")
        {
            return EnableCache(timeSpan)
                ? _cache.GetOrCreateAsync(string.IsNullOrEmpty(cacheKey)?
                    CreateCacheKey(sqlCommandString, parameters): cacheKey,
                    timeSpan,
                    getResultFunc)
                : getResultFunc();
        }
        private Task<TFuncResult> ExecuteCacheOrGetAsync<TFuncResult>(
            string sqlCommandString,
            IDbDataParameter[] parameters,
            Func<Task<TFuncResult>> getResultFunc,
            TimeSpan? timeSpan,
             string cacheKey = "")
        {
            return EnableCache(timeSpan)
                ? _cache.GetOrCreateAsync(string.IsNullOrEmpty(cacheKey) ?
                    CreateCacheKey(sqlCommandString, parameters): cacheKey,
                    timeSpan,
                    getResultFunc)
                : getResultFunc();
        }
        
        private TFuncResult ExecuteCacheOrGet<TFuncResult>(
            string sqlCommandString,
            IDbDataParameter[] parameters,
            Func<TFuncResult> getResultFunc,
            TimeSpan? timeSpan)
        {
            return EnableCache(timeSpan)
                ? _cache.GetOrCreate(
                    CreateCacheKey(sqlCommandString, parameters),
                    timeSpan,
                    getResultFunc)
                : getResultFunc();
        }
    }
}
