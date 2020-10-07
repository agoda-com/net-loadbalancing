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
    }
}
