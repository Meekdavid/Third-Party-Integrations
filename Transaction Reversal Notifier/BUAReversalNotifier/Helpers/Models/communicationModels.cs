using System;
using System.Collections.Generic;
using System.Text;

namespace MbokoReversalNotifier.Helpers.Models
{
    public class PaymentRequest
    {
        public string debitaccountnumber { get; set; }
        public string creditaccountnumber { get; set; }
        public decimal amount { get; set; }
        public string name { get; set; }
        public string bank_transaction_id { get; set; }
    }


    public class PaymentResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public int transactionid { get; set; }
    }
}
