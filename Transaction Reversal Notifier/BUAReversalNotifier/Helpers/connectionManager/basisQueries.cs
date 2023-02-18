using System;
using System.Collections.Generic;
using System.Text;

namespace MbokoReversalNotifier.Helpers.connectionManager
{
    public class MbokoQueries
    {
        public const string Query_GetAccountNameFromMboko = " select get_name(man_code, pur_num,0,0,0) Name from dev_acct where dev_acct_no = :nuNo";
        public const string Query_ConvertNubanToOldAccount = " select get_name(man_code, pur_num,0,0,0) Name from dev_acct where dev_acct_no = :nuNo";
        public const string Query_ConvertOldAccountToNuban = " select get_name(man_code, pur_num,0,0,0) Name from dev_acct where dev_acct_no = :nuNo";
    }
}