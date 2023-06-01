using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using ResponseModels;
using System.Net;
using RestSharp;
using System.Net.Http;
using System.Text;
effi
public class GEP
{
    private string cementValURL;
    private string cementPayURL;
    private string foodValURL;
    private string foodPayURL;
    private string token;
    private string category;
    private string url;
    private string mbokoNumber;
    protected static System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

    public GEP()
    {
    }

    public string GetFieldDetailsGEP(DataSet dt_set, string branch, int formid)
    {
        string refId = string.Empty;
        string customerName = string.Empty;
        string emailAddress = string.Empty;
        string phoneNum = string.Empty;
        string ret_str = string.Empty;
        string StatusMess = string.Empty;
        string classMethod = "BUA";
        string hash = string.Empty;
        string amount = string.Empty;


        try
        {
            DataTable dt = dt_set.Tables[0];
            cementValURL = ConfigurationManager.AppSettings["GEPCement_Validate_URL"].ToString();
            foodValURL = ConfigurationManager.AppSettings["GEPFood_Validate_URL"].ToString();
            token = ConfigurationManager.AppSettings["GEPToken"].ToString();

            foreach (DataRow drr in dt.Rows)
            {
                if (drr["FieldName"].ToString().ToUpper().Trim() == "Order ID/Dealer ID".ToString().ToUpper().Trim())
                {
                    refId = drr["Value"].ToString();
                }

                if (drr["FieldName"].ToString().ToUpper().Trim() == "Product Category".ToString().ToUpper().Trim())
                {
                    category = drr["Value"].ToString();
                }

                if (drr["FieldName"].ToString().ToUpper().Trim() == "Amount".ToString().ToUpper().Trim())
                {
                    amount = drr["Value"].ToString();
                }
            }

            if (!isNumeric(amount))
            {
                return "Invalid Amount";

            }

            if (category == "Cement")
            {
                url = cementValURL;
            }
            else
            {
                url = foodValURL;
            }

            if (!string.IsNullOrEmpty(refId))
            {

                Validationrequest jsonObj = new Validationrequest();
                jsonObj.orderid = refId;

                string jsonString = JsonConvert.SerializeObject(jsonObj);
                ErrHandler.Log(classMethod, refId, "Validation Request sent to GEP for " + refId + " is: " + jsonString);
                string Response = HttpPostRestClientToken(token, jsonString, url, false, null);
                ErrHandler.Log(classMethod, refId, "Validation Response from GEP for " + refId + " is: " + Response);

                
                if (!string.IsNullOrEmpty(Response))
                {
                    Validateresponse result = JsonConvert.DeserializeObject<Validateresponse>(Response);

                    string statuscode = result.status;
                    string errorMessage = result.message;


                    if (statuscode == "200")
                    {
                        customerName = result.name;

                        foreach (DataRow drr in dt.Rows)
                        {
                            if (drr["FieldName"].ToString().ToUpper().Trim() == "Customer Name".ToUpper().Trim())
                                drr["Value"] = customerName;
                        }

                        ret_str = "SUCCESS";

                    }
                    else
                    {
                        ErrHandler.Log(classMethod, refId, errorMessage);
                        ret_str = errorMessage;
                    }

                }
                else
                {
                    ErrHandler.Log(classMethod, refId, "Empty Response from GEP");
                    ret_str = "Empty Response from GEP";
                }
            }
            else
            {
                ErrHandler.Log(classMethod, refId, "OrderID/Dealer ID Field is Empty");
                ret_str = "OrderID/Dealer ID Field is Empty";
            }
        }

        catch (Exception ex)
        {
            ErrHandler.Log(classMethod, refId, ex.Message);
            return ex.Message;
        }

        return ret_str;

    }

    public string UpdateFieldDetailsGEP(string coll_acctno, DataSet dt_data, string org_branch, string transref, DateTime transdate, int payment_mode, int formid, long transid)
    {
        string ClassMeth = "GEP|PaymentUpdate";
        Utilities util = new Utilities();
        string refId = string.Empty;
        string insuredName = string.Empty;
        string totalAmount = string.Empty;
        string hash = string.Empty;
        string PayRef = string.Empty;
        string RespDesc = string.Empty;
        string resApi = string.Empty;
        DataTable dt = dt_data.Tables[0];
        string return_str = string.Empty;
        string Currency = "566";



        try
        {
            cementPayURL = ConfigurationManager.AppSettings["GEPCement_Payment_URL"].ToString();
            foodPayURL = ConfigurationManager.AppSettings["GEPFood_Payment_URL"].ToString();
            token = ConfigurationManager.AppSettings["BUAToken"].ToString();



            foreach (DataRow drr in dt.Rows)
            {
                if (drr["field_name"].ToString().ToUpper() == "Order ID/Dealer ID".ToUpper())
                    refId = drr["trans_value"].ToString().Trim();
                if (drr["field_name"].ToString().ToUpper() == "Product Category".ToUpper())
                    category = drr["trans_value"].ToString().Trim();
                if (drr["field_name"].ToString().ToUpper() == "Amount".ToUpper())
                    totalAmount = drr["trans_value"].ToString().Trim();
            }

            if (category == "Cement")
            {
                url = cementPayURL;

            }
            else
            {
                url = foodPayURL;
            }


   

            Paymentupdaterequest JsonObj = new Paymentupdaterequest();
            JsonObj.orderid = refId;
            JsonObj.creditaccountnumber = accountNumber;
            JsonObj.amount = totalAmount;
            JsonObj.bank_transaction_id = transref;

            string jsonString = JsonConvert.SerializeObject(JsonObj);
            ErrHandler.Log(ClassMeth, refId, "Payment Update Request sent to GEP is: " + jsonString);
            string Response = HttpPostRestClientToken(token, jsonString, url, false, null);
            ErrHandler.Log(ClassMeth, refId, "Payment Update Response from GEP is: " + Response);

            if (!string.IsNullOrEmpty(Response))
            {
                Paymentupdateresponse response = JsonConvert.DeserializeObject<Paymentupdateresponse>(Response);

                string RespCode = response.status;
                RespDesc = response.message;
                PayRef = response.transactionid;

                if (RespCode == "200")
                {
                    
                        
                        updateThirdPartyReference(transid, PayRef);

                        ci = null;
                        return_str = "SUCCESS";
                }
                else
                {
                    ErrHandler.Log(ClassMeth, refId, RespDesc);
                    return_str = RespDesc;
                }
            }
            else
            {
                ErrHandler.Log(ClassMeth, refId, "Empty Response from GEP");
                return_str = "Empty Response from GEP";
            }
        }

        catch (Exception ex)
        {

            ErrHandler.Log(ClassMeth, "", ex.Message);
        }

        return return_str;
    }

    public string HttpPostRestClientToken(string token, string parameter, string url, bool usenameVParm, objectMultiSelect nameVParam)
    {
        
        string classMethodName = "Utilities|HttpPostRestClient";


        try
        {
            var client = new RestClient(url);


            
            var request = new RestRequest(Method.POST);

            if (usenameVParm == false)
            {
                request.AddHeader("content-type", "application/json");
                request.AddHeader("Authorization", "Bearer " + token);
                request.AddParameter("application/json", parameter, ParameterType.RequestBody);
            }
            else
            {
                for (int i = 0; i < nameVParam.sItem.Length; i++)
                {
                    request.AddParameter(nameVParam.sItem[i], nameVParam.sValue[i]);
                }
            }

            var restResponse = client.Execute(request);
            if (string.IsNullOrEmpty(restResponse.Content))
            {
                ErrHandler.WriteError(" Response content is empty, Parameter is  " + parameter + " URL is " + url + " Error is " + restResponse.ErrorException + " Error Message is " + restResponse.ErrorException);
                // return restResponse.ErrorMessage;
            }

            return restResponse.Content;
        }
        catch (Exception ex)
        {
            ErrHandler.Log(classMethodName, url, ex.Message);
            return "Get customer info failed.";
        }
    }



    public class Validationrequest
    {
        public string orderid { get; set; }
    }

    public class Validateresponse
    {

        public string status { get; set; }
        public string message { get; set; }
        public string name { get; set; }
        public string amount { get; set; }

    }

    public class Paymentupdaterequest
    {
        public string orderid { get; set; }
        public string creditaccountnumber { get; set; }
        public string amount { get; set; }
        public string bank_transaction_id { get; set; }
    }


    public class Paymentupdateresponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public string transactionid { get; set; }
     }
