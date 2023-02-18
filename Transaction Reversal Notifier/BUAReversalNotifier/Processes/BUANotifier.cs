using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using MbokoReversalNotifier.Interfaces;
using System.Data;
using MbokoReversalNotifier.Helpers.Models;
using System.Threading.Tasks;
using System.Data.SqlClient;
using MbokoReversalNotifier.Helpers.ConfigurationSettinigs.AppSettings;
using MbokoReversalNotifier.Helpers.connectionManager;
using AutoMapper;
using Serilog.AspNetCore;

namespace MbokoReversalNotifier.Processes
{
    public class MbokoNotifier
    {
        private readonly ILogger<Worker> _logger;
        public readonly IConfiguration _configs;
        private readonly ISqlProcess _sqlProcess;
        private readonly IMapper _mapper;
        private readonly IProtocolHandler _protocolHandler;
        private string narration;
        private string transRef;
        private string formID;
        private string amount;
        private string customerAccount;
        private string MbokoAccount;

        public MbokoNotifier(IConfiguration configs, ILogger<Worker> logger, ISqlProcess sqlProcess, IProtocolHandler protocolHandler)
        => (configs, logger, sqlProcess, protocolHandler) = (configs, logger, sqlProcess, protocolHandler);


        public async Task<string> GetTransactions()
        {
            string classmethod = "GetTransactions";
            string returnMessage = string.Empty;
            bool isSucces = false;
            _logger.LogInformation($"\r\n\r\n\r\n--------------Beginning the execution of {classmethod}----------------");
            try
            {
                DataTable dataTable = new DataTable();

                _logger.LogInformation($"About retrieving Mboko transactions from transactions table with the stored procedure 'SelectMbokoTransactions'");
                var Mbokotransactions = await _sqlProcess.sqlSelectQuery("GetTransactions", ConfigSettings.ConnectionString.MbokoDbConection, "SelectMbokoTransactions", CommandType.StoredProcedure);

                if (Mbokotransactions.queryIsSuccessful)
                {
                    var transactionDetails = Mbokotransactions.objectValue;
                    if (transactionDetails != null)
                    {
                        foreach (DataRow Mbokorow in transactionDetails.Rows)
                        {
                            _logger.LogInformation($"Collecting transaction details for the table with total rows: {transactionDetails.Rows.Count}");
                            narration = Mbokorow["narration"].ToString();
                            transRef = Mbokorow["reference"].ToString();
                            formID = Mbokorow["form_id"].ToString();
                            amount = Mbokorow["tra_amt_mert"].ToString();
                            customerAccount = Mbokorow["debit_acct"].ToString();
                            MbokoAccount = Mbokorow["coll_acct"].ToString();
                            _logger.LogInformation($"The following details have been collected: \r\n\r\nNarration: {narration}\r\nTransref: {transRef}\r\nFormID: {formID}\r\nAmount: ₦{amount}\r\nCustomer Account: {customerAccount}\r\nMboko Account: {MbokoAccount}");

                            string customerNuban = await _sqlProcess.convertToNuban(classmethod, customerAccount);
                            string customerName = await _sqlProcess.getCustomerName(classmethod, customerNuban);
                            string MbokoNuban = await _sqlProcess.convertToNuban(classmethod, MbokoAccount);
                            if (customerNuban == "ERROR" || customerName == "ERROR" || MbokoNuban == "ERROR")
                            {
                                //Should in case Mboko fails, shutdown and await
                                return "ERROR";
                            }

                            //Check if this transaction was reversed
                            var customerAccountEntity = getAccountAttributes(customerAccount);
                            var MbokoaccountEntity = getAccountAttributes(MbokoAccount);
                            var checkQueryMbokoTellAcct = "select * from tell_act where Location_ID = " + MbokoaccountEntity.Location_ID + " and Customer_ID = " + MbokoaccountEntity.Customer_ID + " and Currency_ID = " + MbokoaccountEntity.Currency_ID + " and Ledger_ID = " + MbokoaccountEntity.Ledger_ID + " and remarks like '%" + transRef + "%'";
                            var checkQueryCustomerTellAcct = "select * from tell_act where Location_ID = " + customerAccountEntity.Location_ID + " and Customer_ID = " + customerAccountEntity.Customer_ID + " and Currency_ID = " + customerAccountEntity.Currency_ID + " and Ledger_ID = " + customerAccountEntity.Ledger_ID + " and remarks like '%" + transRef + "%'";
                            var checkQueryMbokoTransact = "select * from transact where Location_ID = " + MbokoaccountEntity.Location_ID + " and Customer_ID = " + MbokoaccountEntity.Customer_ID + " and Currency_ID = " + MbokoaccountEntity.Currency_ID + " and Ledger_ID = " + MbokoaccountEntity.Ledger_ID + " and remarks like '%" + transRef + "%'";
                            var checkQueryCustomerTransact = "select * from tell_act where Location_ID = " + customerAccountEntity.Location_ID + " and Customer_ID = " + customerAccountEntity.Customer_ID + " and Currency_ID = " + customerAccountEntity.Currency_ID + " and Ledger_ID = " + customerAccountEntity.Ledger_ID + " and remarks like '%" + transRef + "%'";

                            //Checking Mboko for possibile reversals
                            _logger.LogInformation($"About checkinig Mboko for potential reversals for Transaction Reference {transRef}");
                            var MbokoTellAcctCheck = await _sqlProcess.sqlSelectQueryMboko(classmethod, ConfigSettings.ConnectionString.MbokoDbConection, checkQueryMbokoTellAcct, CommandType.Text);
                            var customerTellAcctCheck = await _sqlProcess.sqlSelectQueryMboko(classmethod, ConfigSettings.ConnectionString.MbokoDbConection, checkQueryCustomerTellAcct, CommandType.Text);
                            var MbokoTransactCheck = await _sqlProcess.sqlSelectQueryMboko(classmethod, ConfigSettings.ConnectionString.MbokoDbConection, checkQueryMbokoTellAcct, CommandType.Text);
                            var customerTransactCheck = await _sqlProcess.sqlSelectQueryMboko(classmethod, ConfigSettings.ConnectionString.MbokoDbConection, checkQueryCustomerTellAcct, CommandType.Text);

                            //First check tell_acct table
                            if ((MbokoTellAcctCheck.queryIsSuccessful && MbokoTellAcctCheck.reversal == true) && (customerTellAcctCheck.queryIsSuccessful && customerTellAcctCheck.reversal == true))
                            {
                                _logger.LogInformation($"Reversal is found on Tell_Acct for customer account {customerNuban} with transaction reference {transRef}");

                                //Let us now notify Mboko of the reversal
                                PaymentRequest paymentUpdate = getPaymentRequest(customerAccount, MbokoAccount, amount, customerName, transRef);
                                string MbokoResponse = await Notifier(paymentUpdate);
                                if (MbokoResponse.ToString().ToUpper() == "SUCCESS")
                                {
                                    //Let us update the transaction table to convey that Mboko has been notified
                                    SqlParameter[] Params = new SqlParameter[]
                                    {
                                    new SqlParameter("@reference",transRef)
                                    };
                                    var updateResult = await _sqlProcess.sqlInsert_UpdateQuery(classmethod, ConfigSettings.ConnectionString.MbokoDbConection, "UpdateMbokoTransactions", CommandType.StoredProcedure, Params);
                                    if (updateResult.queryIsSuccessful)
                                    {
                                        _logger.LogInformation($"Transaction table successfully updated for transaction reference {transRef}");
                                        isSucces = true;
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"Error occured while updating transactions table for transaction reference {transRef}");
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation($"Mboko was not sucessfully updated on the reversal for customer account {customerNuban} with reference {transRef}");
                                }

                            }
                            else if ((MbokoTransactCheck.queryIsSuccessful && MbokoTransactCheck.reversal == true) && (customerTransactCheck.queryIsSuccessful && customerTransactCheck.reversal == true))//No reversal was found on tell_act, we are here to investigate Transact too.
                            {
                                _logger.LogInformation($"Reversal is found on Transact for customer account {customerNuban}");
                                //Let us now notify Mboko of the reversal
                                PaymentRequest paymentUpdate = getPaymentRequest(customerAccount, MbokoAccount, amount, customerName, transRef);
                                string MbokoResponse = await Notifier(paymentUpdate);
                                if (MbokoResponse.ToString().ToUpper() == "SUCCESS")
                                {
                                    //Let us update the transaction table to convey that Mboko has been notified
                                    SqlParameter[] Params = new SqlParameter[]
                                    {
                                    new SqlParameter("@reference",transRef)
                                    };
                                    var updateResult = await _sqlProcess.sqlInsert_UpdateQuery(classmethod, ConfigSettings.ConnectionString.MbokoDbConection, "UpdateMbokoTransactions", CommandType.StoredProcedure, Params);
                                    if (updateResult.queryIsSuccessful)
                                    {
                                        _logger.LogInformation($"Transaction table successfully updated for transaction reference {transRef}");
                                        isSucces = true;
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"Error occured while updating transactions table for transaction reference {transRef}");
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation($"Mboko was not sucessfully updated on the reversal for customer account {customerNuban} with reference {transRef}");
                                }
                            }
                            else
                            {
                                //No reversals where found, No need notifying Mboko.
                                _logger.LogInformation($"No reversals found for customer account {customerNuban} with reference {transRef} \r\n\r\n\r\n");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("There are no current records for Mboko on the Transactions Table\r\n\r\n\r\n");
                    }
                }
                else
                {
                    _logger.LogInformation("Error occured While Retrieving RecordS from Mboko Transaction Table\r\n\r\n\r\n");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"{transRef} An Exception occured:{ex.Message}, {ex.StackTrace}, {ex.InnerException?.Message}\r\n\r\n\r\n");
                return "ERROR";
            }

            return isSucces ? "SUCCESS" : "ERROR";

        }

        public oldAccountMembers getAccountAttributes(string oldAccountNumber)
        {
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

            var attri = new oldAccountMembers();
            attri.Currency_ID = Currency_ID;
            attri.Location_ID = Location_ID;
            attri.Customer_ID = Customer_ID;
            attri.Ledger_ID = Ledger_ID;
            attri.Supplementary_ID = Supplementary_ID;

            return attri;
        }

        public PaymentRequest getPaymentRequest(string customerAccount, string MbokoAccount, string amount, string customerName, string transRef)
        {
            PaymentRequest paymentRequest = new PaymentRequest();
            paymentRequest.debitaccountnumber = customerAccount;
            paymentRequest.creditaccountnumber = MbokoAccount;
            paymentRequest.amount = Decimal.Parse(amount);
            paymentRequest.name = $"{customerName} -RVS";
            paymentRequest.bank_transaction_id = $"{transRef} -RVS";

            return paymentRequest;
        }

        public async Task<string> Notifier(PaymentRequest request)
        {
            string classMethod = "paymentUpdateNotifier";
            string legacyUrl = (request.creditaccountnumber == ConfigSettings.webConfigAttributes.FoodAccount) ? ConfigSettings.webConfigAttributes.FoodLegacyUrl : ConfigSettings.webConfigAttributes.CementLegacyUrl;
            string ret_str;
            string jsonString = JsonConvert.SerializeObject(request);
            _logger.LogInformation("Payment Update Request sent to MbokoLegacy is: " + jsonString);
            var resApi = await _protocolHandler.HttpPostMethod(classMethod, ConfigSettings.webConfigAttributes.Token, jsonString, legacyUrl, false, null);
            _logger.LogInformation("Payment Update Response from MbokoLegacy is: " + resApi);
            if (!string.IsNullOrEmpty(resApi))
            {

                PaymentResponse resp = JsonConvert.DeserializeObject<PaymentResponse>(resApi);

                if (resp != null)
                {
                    if (resp.status == "200")
                    {
                        ret_str = "SUCCESS";
                    }
                    else
                    {
                        _logger.LogInformation($"Invalid Status Code {resp.message}");
                        ret_str = resp.message;
                    }
                }
                else
                {
                    _logger.LogInformation("Payment response Object from MbokoLegacy is null");
                    ret_str = "Payment response Object from MbokoLegacy is empty";
                }
            }
            else
            {
                _logger.LogInformation("Payment Response from MbokoLegacy is Empty.");
                ret_str = "Payment Response from MbokoLegacy is Empty.";
            }
            return ret_str;
        }
    }
}
