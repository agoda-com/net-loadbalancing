using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Agoda.Frameworks.LoadBalancing;
using Dapper;


namespace Agoda.Frameworks.DB
{
    public partial class DbRepository // new simplified stuff
    {
        public async Task<object> ExecuteScalarAsync(
            string dbName,
            string SqlCommandString,
            CommandType commandType,
            object param)
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
                        SqlCommandString,
                        param,
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
                RaiseOnQueryComplete(new IAmNotAStoredProc(dbName, SqlCommandString, DefaultTimeoutSec, DefaultMaxAttempts), stopwatch.ElapsedMilliseconds, error);
            }
        }

        public async Task<object> ExecuteQueryAsync<T>(
            string dbName,
            string SqlCommandString,
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
                        SqlCommandString,
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
                RaiseOnQueryComplete(new IAmNotAStoredProc(dbName, SqlCommandString, DefaultTimeoutSec, DefaultMaxAttempts), stopwatch.ElapsedMilliseconds, error);
            }
        }

        public async Task<object> ExecuteQuerySingleAsync<T>(
            string dbName,
            string SqlCommandString,
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
                        SqlCommandString,
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
                RaiseOnQueryComplete(new IAmNotAStoredProc(dbName, SqlCommandString, DefaultTimeoutSec, DefaultMaxAttempts), stopwatch.ElapsedMilliseconds, error);
            }
        }
    }
}
