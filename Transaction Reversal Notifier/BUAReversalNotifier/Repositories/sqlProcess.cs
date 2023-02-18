using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using MbokoReversalNotifier.Helpers.Models;
using MbokoReversalNotifier.Interfaces;
using MbokoReversalNotifier.Helpers.connectionManager;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using System.Reflection;
using MbokoReversalNotifier.Helpers.ConfigurationSettinigs.AppSettings;
using MbokoReversalNotifier.Helpers.ConfigurationSettinigs;
using System.Configuration;
using Newtonsoft.Json;

namespace MbokoReversalNotifier.Repositories
{
    public class sqlProcess : ISqlProcess
    {
        private readonly ILogger<sqlProcess> _logger;
        public sqlProcess(ILogger<sqlProcess> logger) => (_logger) = (logger);

        public async Task<string> convertToNuban(string destination, string oldAccountNumber)
        {
            string classMeth = "convertToNuban";
            _logger.LogInformation($"\r\n\r\n\r\n--------------Switched from {destination} to {classMeth}----------------");
            char acctsplit = Convert.ToChar("/");
            string[] accountkey = new string[4];
            string Location_ID = null;
            string Customer_ID = null;
            string Currency_ID = null;
            string Ledger_ID = null;
            string Supplementary_ID = null;

            accountkey = oldAccountNumber.Trim().Split(acctsplit);
            Location_ID = accountkey[0];
            Customer_ID = accountkey[1];
            Currency_ID = accountkey[2];
            Ledger_ID = accountkey[3];
            Supplementary_ID = accountkey[4];

            string NUBAN = null;
            int result;

            string selectquery = "select  Wallet_NO from Wallett where Location_ID = " + Location_ID + " and Customer_ID = " + Customer_ID + "and Currency_ID = " + Currency_ID + "and Ledger_ID = " + Ledger_ID + " and Supplementary_ID = " + Supplementary_ID;

            _logger.LogInformation($"About Opening Oracle connection");
            using (OracleConnection OraConn = sqlManager.OracleDatabaseCreateConnection(ConfigSettings.ConnectionString.MbokoDbConection, true))
            {
                _logger.LogInformation($"Oracle connection successfully opened");
                using OracleCommand OraSelectb = new OracleCommand();
                {
                    try
                    {
                        OraSelectb.Connection = OraConn;
                        OraSelectb.CommandText = selectquery;
                        OraSelectb.CommandType = CommandType.Text;

                        _logger.LogInformation($"About executing the query {selectquery}");
                        result = await OraSelectb.ExecuteNonQueryAsync();
                        _logger.LogInformation($"SQL query executed with rows affected as {result}");

                        using OracleDataReader OraDrSelectb = OraSelectb.ExecuteReader(CommandBehavior.CloseConnection);

                        if (OraDrSelectb.HasRows == true)
                        {

                            while (OraDrSelectb.Read())
                            {
                                NUBAN = OraDrSelectb["Wallet_NO"].ToString();
                                _logger.LogInformation($"Records have been found after executing the query: {selectquery} with Result {NUBAN}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"An Exception occured:{ex.Message}, {ex.StackTrace}, {ex.InnerException?.Message}");
                        return "ERROR";
                    }
                    finally
                    {
                        sqlManager.closeOpenedConnection1(OraConn);
                    }
                    _logger.LogInformation($"Method finished executing successfully with result {NUBAN}");
                }
            }
            _logger.LogInformation($"--------------Switching back to {destination}----------------\r\n\r\n\r\n");
            return NUBAN;
        }

        public async Task<string> getCustomerName(string destination, string Nuban)
        {
            string classMeth = "getCustomerName";
            string customerName = null;
            int result;
            _logger.LogInformation($"\r\n\r\n\r\n--------------Switched from {destination} to {classMeth}----------------");

            string selectquery = "select get_name(Location_ID, Customer_ID,0,0,0) Name from Wallett where Wallet_no =" + Nuban + "";

            _logger.LogInformation($"About to open Mboko to execute {selectquery}");
            using (OracleConnection OraConn = sqlManager.OracleDatabaseCreateConnection(ConfigSettings.ConnectionString.MbokoDbConection, true))
            {
                _logger.LogInformation($"Mboko connection successfully opened");
                using OracleCommand OraSelectb = new OracleCommand();
                {
                    try
                    {
                        OraSelectb.Connection = OraConn;
                        OraSelectb.CommandText = selectquery;
                        OraSelectb.CommandType = CommandType.Text;

                        result = await OraSelectb.ExecuteNonQueryAsync();
                        _logger.LogInformation($"Mboko query executed with rows affected as {result}");

                        using OracleDataReader OraDrSelectb = OraSelectb.ExecuteReader(CommandBehavior.CloseConnection);

                        if (OraDrSelectb.HasRows == true)
                        {

                            while (OraDrSelectb.Read())
                            {
                                customerName = OraDrSelectb.GetValue(0).ToString();
                                _logger.LogInformation($"Records have been found after executing the query: {selectquery} with the Result {customerName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"An Exception occured:{ex.Message}, {ex.StackTrace}, {ex.InnerException?.Message}");
                        return "ERROR";
                    }
                    finally
                    {
                        sqlManager.closeOpenedConnection1(OraConn);
                    }
                    _logger.LogInformation($"Method finished executing successfully with result {customerName}");
                }
            }
            _logger.LogInformation($"--------------Switching back to {destination}----------------\r\n\r\n\r\n");
            return customerName;
        }

        public async Task<databaseResult> sqlInsert_UpdateQuery(string destination, string ConnString, string CommandName, CommandType commandType, SqlParameter[] param)
        {
            string classMeth = "sqlInsertQuery";
            _logger.LogInformation($"\n\r\n\r\n\r\n--------------Switched from {destination} to {classMeth}----------------\n");
            int result = 0;
            var tableResult = new databaseResult();

            using (SqlConnection con = new SqlConnection(ConnString))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        cmd.CommandType = commandType;
                        cmd.CommandText = CommandName;
                        cmd.Parameters.AddRange(param);

                        _logger.LogInformation($"About opening SQL Connection to execute stored procedure: {CommandName}");
                        sqlManager.openSQLConnection(ConnString);

                        result = await cmd.ExecuteNonQueryAsync();
                        tableResult.ResponseCode = (result > 0) ? 200 : 400;
                        tableResult.queryIsSuccessful = (result > 0) ? true : false;
                        tableResult.ResponseMessage = (result > 0) ? "SUCCESS" : "ERROR";
                        _logger.LogInformation($"Procedure: {CommandName} finished executing with result: {result}");
                    }
                    catch (Exception ex)
                    {
                        tableResult.ResponseCode = 400;
                        tableResult.ResponseMessage = "ERROR";
                        tableResult.queryIsSuccessful = false;
                        _logger.LogInformation($"An Exception occured:{ex.Message}, {ex.StackTrace}, {ex.InnerException?.Message}");
                        return tableResult;
                    }
                    finally
                    {
                        sqlManager.closeOpenedConnection1(con);
                    }
                }
            }
            _logger.LogInformation($"\n--------------Switching back to {destination}----------------\r\n\r\n\r\n\n");
            return tableResult;
        }

        public async Task<databaseResult> sqlSelectQuery(string destination, string ConnString, string CommandQuery, CommandType cmdType)
        {
            string classMeth = "sqlSelectQuery ";
            IDbConnection openedConnection = null;
            _logger.LogInformation($"\r\n\r\n\r\n--------------Switched from {destination} to {classMeth}----------------");
            int result = 0;
            DataTable ds = new DataTable();
            var tableResult = new databaseResult();
            try
            {
                _logger.LogInformation($"About to open SQL connection to execute {CommandQuery}");
                using (SqlConnection con = sqlManager.SqlDatabaseCreateConnection(ConnString, true))
                {
                    using (SqlCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandType = cmdType;
                        cmd.CommandText = CommandQuery;
                        cmd.Connection = con;

                        result = await cmd.ExecuteNonQueryAsync();
                        _logger.LogInformation($"SQL query executed with rows affected as {result}");
                        tableResult.ResponseCode = (!(result == 0)) ? 200 : 400;
                        tableResult.queryIsSuccessful = (!(result == 0)) ? true : false;
                        tableResult.ResponseMessage = (!(result == 0)) ? "SUCCESS" : "ERROR";

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(ds);
                            tableResult.objectValue = ds;
                            _logger.LogInformation($"Method finished executing with result: {ds.ToString()}");
                        }
                    }
                    openedConnection = con;
                }
                _logger.LogInformation($"--------------Switching back to {destination}----------------\r\n\r\n\r\n");
                
                return tableResult;
            }
            catch (Exception ex)
            {
                tableResult.ResponseCode = 400;
                tableResult.ResponseMessage = "ERROR";
                tableResult.queryIsSuccessful = false;
                _logger.LogInformation($"An Exception occured:{ex.Message}, {ex.StackTrace}, {ex.InnerException?.Message}");
                return tableResult;
            }
            finally
            {
                sqlManager.closeOpenedConnection1(openedConnection);
            }

        }

        public async Task<oracleDatabaseResult> sqlSelectQueryMboko(string destination, string ConnString, string CommandQuery, CommandType cmdType)
        {
            string classMeth = "MbokoSqlSelectQuery ";
            _logger.LogInformation($"\r\n\r\n\r\n--------------Switched from {destination} to {classMeth}----------------");
            int result = 0;
            DataTable ds = new DataTable();
            var tableResult = new oracleDatabaseResult();

            _logger.LogInformation($"About to open Oracle connection to execute {CommandQuery}");
            using (OracleConnection OraConn = sqlManager.OracleDatabaseCreateConnection(ConnString, true))
            {
                _logger.LogInformation($"Oracle connection successfully opened");
                using OracleCommand OraSelect = new OracleCommand();
                {
                    try
                    {
                        OraSelect.Connection = OraConn;
                        OraSelect.CommandText = CommandQuery;
                        OraSelect.CommandType = CommandType.Text;

                        result = await OraSelect.ExecuteNonQueryAsync();
                        _logger.LogInformation($"SQL query executed with rows affected as {result}");

                        using OracleDataReader OraDrSelect = OraSelect.ExecuteReader(CommandBehavior.CloseConnection);
                        tableResult.ResponseCode = (!(result == 0)) ? 200 : 400;
                        tableResult.queryIsSuccessful = (!(result == 0)) ? true : false;
                        tableResult.ResponseMessage = (!(result == 0)) ? "SUCCESS" : "ERROR";
                        if (OraDrSelect.HasRows == true)
                        {
                            while (OraDrSelect.Read())
                            {
                                using (OracleDataAdapter oracleAdapter = new OracleDataAdapter())
                                {
                                    oracleAdapter.SelectCommand = OraSelect;
                                    oracleAdapter.Fill(ds);
                                    tableResult.objectValue = ds;
                                }
                                _logger.LogInformation($"Records found after executing the query:{CommandQuery}, Result: {ds.ToString()}");

                                if ((ds.Rows[0][13].ToString() == "1" && ds.Rows[1][13].ToString() == "2") || (ds.Rows[0][13].ToString() == "2" && ds.Rows[1][13].ToString() == "1"))
                                {
                                    tableResult.reversal = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        tableResult.ResponseCode = 400;
                        tableResult.ResponseMessage = "ERROR";
                        tableResult.queryIsSuccessful = false;
                        _logger.LogInformation($"An Exception occured:{ex.Message}, {ex.StackTrace}, {ex.InnerException?.Message}");
                        return tableResult;
                    }
                    finally
                    {
                        sqlManager.closeOpenedConnection1(OraConn);
                        _logger.LogInformation($"Method finished executing successfully with Result Reversal = {tableResult.reversal}");
                    }
                }
            }
            _logger.LogInformation($"--------------Switching back to {destination}----------------\r\n\r\n\r\n");
            return tableResult;
        }

        public async Task<oracleDatabaseResult> sqlSelectQueryMbokoWithParams(string destination, string ConnString, string CommandQuery, CommandType cmdType, SqlParameter[] param)
        {
            string classMeth = "MbokoSqlSelectQuery ";
            _logger.LogInformation($"\r\n\r\n\r\n--------------Switched from {destination} to {classMeth}----------------");
            int result = 0;
            DataTable ds = new DataTable();
            var tableResult = new oracleDatabaseResult();

            _logger.LogInformation($"About to open Oracle connection to execute {CommandQuery}");
            using (OracleConnection OraConn = sqlManager.OracleDatabaseCreateConnection(ConnString, true))
            {
                _logger.LogInformation($"Oracle connection successfully opened");
                using OracleCommand OraSelect = new OracleCommand();
                {
                    try
                    {
                        OraSelect.Connection = OraConn;
                        OraSelect.CommandText = CommandQuery;
                        OraSelect.CommandType = CommandType.Text;
                        OraSelect.Parameters.AddRange(param);

                        result = await OraSelect.ExecuteNonQueryAsync();
                        _logger.LogInformation($"SQL query executed with rows affected as {result}");

                        using OracleDataReader OraDrSelect = OraSelect.ExecuteReader(CommandBehavior.CloseConnection);
                        tableResult.ResponseCode = (OraDrSelect.HasRows == true) ? 200 : 400;
                        tableResult.queryIsSuccessful = (OraDrSelect.HasRows == true) ? true : false;
                        tableResult.ResponseMessage = (OraDrSelect.HasRows == true) ? "SUCCESS" : "ERROR";
                        if (OraDrSelect.HasRows == true)
                        {

                            while (OraDrSelect.Read())
                            {
                                using (OracleDataAdapter oracleAdapter = new OracleDataAdapter())
                                {
                                    oracleAdapter.SelectCommand = OraSelect;
                                    oracleAdapter.Fill(ds);
                                    tableResult.objectValue = ds;
                                    _logger.LogInformation($"Records have been found after executing the query: {CommandQuery},  Result: {ds.ToString()}");
                                }

                                if ((ds.Rows[0][13].ToString() == "1" && ds.Rows[1][13].ToString() == "2") || (ds.Rows[0][13].ToString() == "2" && ds.Rows[1][13].ToString() == "1"))
                                {
                                    tableResult.reversal = true;
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        tableResult.ResponseCode = 400;
                        tableResult.ResponseMessage = "ERROR";
                        tableResult.queryIsSuccessful = false;
                        _logger.LogInformation($"An Exception occured:{ex.Message}, {ex.StackTrace}, {ex.InnerException?.Message}");
                        return tableResult;
                    }
                    finally
                    {
                        sqlManager.closeOpenedConnection1(OraConn);
                        _logger.LogInformation($"Method finished executing successfully with Reversal as {tableResult.reversal}");
                    }
                }
            }
            _logger.LogInformation($"--------------Switching back to {destination}----------------\r\n\r\n\r\n");
            return tableResult;
        }
    }
}
