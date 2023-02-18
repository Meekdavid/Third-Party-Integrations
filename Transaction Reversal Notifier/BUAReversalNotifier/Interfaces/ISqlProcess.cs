using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MbokoReversalNotifier.Helpers.Models;
using System.Threading.Tasks;

namespace MbokoReversalNotifier.Interfaces
{
    public interface ISqlProcess
    {
        public Task<databaseResult> sqlSelectQuery(string destination, string ConnString, string CommandQuery, CommandType cmdType);
        public Task<oracleDatabaseResult> sqlSelectQueryMboko(string destination, string ConnString, string CommandQuery, CommandType cmdType);
        public Task<oracleDatabaseResult> sqlSelectQueryMbokoWithParams(string destination, string ConnString, string CommandQuery, CommandType cmdType, SqlParameter[] param);
        public Task<databaseResult> sqlInsert_UpdateQuery(string destination, string ConnString, string CommandName, CommandType commandType, SqlParameter[] param);
        public Task<string> convertToNuban(string destination, string oldAccountNumber);
        public Task<string> getCustomerName(string destination, string Nuban);
    }
}
