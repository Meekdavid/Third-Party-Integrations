using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace MbokoReversalNotifier.Helpers.Models
{
    public class databaseResult
    {
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DataTable objectValue { get; set; }
        public bool queryIsSuccessful { get; set; } = true; 
    }

    public class oracleDatabaseResult
    {
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DataTable objectValue { get; set; }
        public bool queryIsSuccessful { get; set; } = true;
        public bool reversal { get; set; } = false;
    }
}
