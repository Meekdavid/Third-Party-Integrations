using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using RestSharp;
using System.Web;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UACSoapname;
using System.Configuration;
using System.Text.RegularExpressions;


/// <summary>
/// Summary description for UAC
/// </summary>
public class UAC
{
    string UACVal = string.Empty;
    string UACPay = string.Empty;
    String UACUserN = string.Empty;
    String UACPassW = string.Empty;
    public UAC()
    {
        try
        {
            UACVal = ConfigurationManager.AppSettings["UACVal"];
            UACPay = ConfigurationManager.AppSettings["UACPay"];
            UACUserN = ConfigurationManager.AppSettings["UACUserN"];
            UACPassW = ConfigurationManager.AppSettings["UACPassW"];
            //apiPassW = apiPassW.Replace('l', '<');
        }
        catch (Exception ex)
        {
            ErrHandler.WriteError(ex.Message + " || UACPAYMENT ");
        }
    }

    public string GetFieldDetailsUAC(DataSet dt_set, string branch, int formid)
    {
        string customerCode = string.Empty;
        //string compCode = string.Empty;
        string customerName = string.Empty;
        string address = string.Empty;
        string transId = string.Empty;
        string customerExpos = string.Empty;
        string ClassMeth = "UACValidation||GetFieldDetailUAC";
        string refId = string.Empty, xmlString = string.Empty, xmlResponse = string.Empty, theReturner = string.Empty, amount = string.Empty;
        try
        {
            DataTable dt = dt_set.Tables[0];

            foreach (DataRow drr in dt.Rows)
            {
                if (drr["fieldname"].ToString().ToUpper().Trim() == "Business Partner No".ToString().ToUpper().Trim())
                    customerCode = drr["Value"].ToString();

                if (drr["fieldname"].ToString().ToUpper().Trim() == "Amount".ToString().ToUpper().Trim())
                    amount = drr["Value"].ToString();
            }

            if (!Utilities.isNumericSpace(customerCode) || !Utilities.isNumericSpace(amount))
            {
                return "Input not valid";
            }

            if (!string.IsNullOrEmpty(customerCode))
            {
                string result = string.Empty;
                DataSet apiResponse = new DataSet();
                var apiMResponse = new XmlDocument();

                try
                {

                    xmlString = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"\r\nxmlns:v1=\"http://uac.com/sap/s4/bank/v1\">\r\n<soapenv:Header/>\r\n<soapenv:Body>\r\n<v1:Bank_BP_Source>\r\n<BusinessPartner_No>" + customerCode + "</BusinessPartner_No>\r\n</v1:Bank_BP_Source >\r\n</soapenv:Body>\r\n</soapenv:Envelope>";
                    xmlResponse = HttpPostUAC(xmlString, UACVal, "XML", false, null);
                    ErrHandler.Log(ClassMeth, customerCode, "Validation Response from UAC for " + customerCode + " is: " + xmlResponse);
                    ErrHandler.WriteError("Validation Response Received From UAC For " + customerCode + " Is: " + xmlResponse);

                    if ((!string.IsNullOrEmpty(xmlResponse)))
                    {
                        apiMResponse.LoadXml(xmlResponse);
                        var node = apiMResponse.GetElementsByTagName("Result");
                        apiMResponse = new XmlDocument();
                        apiMResponse.LoadXml(node[0].OuterXml);
                        XmlElement root = apiMResponse.DocumentElement;
                        string status = root.ChildNodes[0].InnerText;
                        string cusFullName = root.ChildNodes[2].InnerText;

                       
                        if (status == "200")
                        {
                            customerName = cusFullName;

                            foreach (DataRow drr in dt.Rows)
                            {
                                if (drr["FieldName"].ToString().ToUpper().Trim() == "Customer Name".ToUpper().Trim())
                                    drr["Value"] = customerName;
                            }

                            ErrHandler.Log(ClassMeth, customerCode, "Validation Completed and information retrieved for " + customerCode + " is: " + cusFullName);
                            ErrHandler.WriteError("Validation Completed From UAC For " + customerCode + " Is: " + cusFullName);
                            theReturner = "SUCCESS";

                        }
                        else if (status == "404")
                        {
                            string statusMessage = root.ChildNodes[1].InnerText;
                            ErrHandler.Log(ClassMeth, customerCode, "Error encountered for " + customerCode + " is: " + statusMessage);
                            ErrHandler.WriteError("Error encountered for " + customerCode + " Is: " + statusMessage);
                            theReturner = statusMessage;
                        }
                        else
                        {
                            var errorNode = apiMResponse.GetElementsByTagName("faultstring");                         
                            apiMResponse.LoadXml(errorNode[0].OuterXml);
                            XmlElement errorRoot = apiMResponse.DocumentElement;
                            string errorMessage = errorRoot.ChildNodes[0].InnerText;
                            ErrHandler.Log(ClassMeth, refId, errorMessage);
                            ErrHandler.WriteError("Error while trying to retrieve details for " + customerCode + " Is: " + errorMessage);
                            theReturner = errorMessage;
                        }

                    }
                    else
                    {
                        theReturner = "Empty response from UAC";
                        ErrHandler.Log(ClassMeth, refId, theReturner);
                        ErrHandler.WriteError("Error while trying to retrieve details for " + customerCode + " Is: " + theReturner);                      
                    }

                }
                catch (Exception ex)
                {
                    ErrHandler.Log("UACPAY|GetFieldDetails", customerCode, ex.Message);
                    ErrHandler.WriteError("Error while trying to retrieve details for " + customerCode + " Is: " + ex.Message);
                    theReturner = ex.Message;
                }
            }
            else
            {
                ErrHandler.Log(ClassMeth, customerCode, "Partner No Field is Empty");
                theReturner = "Partner No is Empty";
            }
        }
        catch (Exception ex)
        {
            ErrHandler.Log("UACVAL|GetFieldDetails", customerCode, ex.Message);
            ErrHandler.WriteError("Error Retrieving Transaction Details: " + ex.Message.Replace("'", ""));
            theReturner = ex.Message;
        }
        return theReturner;

    }

    public string UpdateFieldDetailsUAC(string coll_acctno, DataSet dt_data, string org_branch, string transref, DateTime transdate, int payment_mode, int formid, long transid)
    {
        string ClassMeth = "UAC|PaymentUpdate";
        Utilities util = new Utilities();
        string customerCode = string.Empty;
        string PayRef = transref;
        DateTime date = transdate;
        string Ndate = date.ToString("dd/MM/yyyy");
        string currency = "NGN";
        string BankName = "GTB";
        string bankAccount = util.ConvertToNuban(coll_acctno);
        string companyCode = "";
        string documentType = "Payment Update";
        string amount = string.Empty;
        string itemText = string.Empty;
        DataTable dt = dt_data.Tables[0];
        string refId = string.Empty, xmlString = string.Empty, xmlResponse = string.Empty, theReturner = string.Empty;

        try
        {

            foreach (DataRow drr in dt.Rows)
            {               
                if (drr["field_name"].ToString().ToUpper() == "Business Partner No".ToUpper())
                    customerCode = drr["trans_value"].ToString().Trim();
                if (drr["field_name"].ToString().ToUpper() == "Item".ToUpper())
                    itemText = drr["trans_value"].ToString().Trim();
                if (drr["field_name"].ToString().ToUpper() == "Amount".ToUpper())
                    amount = drr["trans_value"].ToString().Trim();
            }


            string result = string.Empty;
            DataSet apiResponse = new DataSet();
            var apiMResponse = new XmlDocument();

            try
            {                
                xmlString = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"\r\nxmlns:v1=\"http://uac.com/sap/s4/bank/v1.0\">\r\n<soapenv:Header/>\r\n<soapenv:Body>\r\n<v1:Recv_Bank_CustomerDownPayment>\r\n<Z_Bank_CustomerDownPayment>\r\n<Customer_Nos>" + customerCode + "</Customer_Nos>\r\n<Item_Text>" + itemText +"</Item_Text>\r\n<Posting_Date>" + Ndate + "</Posting_Date>\r\n<Amount>" + amount +"</Amount>\r\n<Currency>" + currency +"</Currency>\r\n<TransReference>" + PayRef +"</TransReference>\r\n<Bank_Name>" + BankName +"</Bank_Name>\r\n<Bank_Account_nos>" + bankAccount +"</Bank_Account_nos>\r\n<Company_Code></Company_Code>\r\n<Document_Type></Document_Type>\r\n</Z_Bank_CustomerDownPayment>\r\n</v1:Recv_Bank_CustomerDownPayment></soapenv:Body>";

                xmlResponse = HttpPostUAC(xmlString, UACPay, "XML", false, null);
                ErrHandler.Log(ClassMeth, customerCode, "Payment Update Response from UAC for " + customerCode + " is: " + xmlResponse);
                ErrHandler.WriteError("Payment Update Response Received From UAC For " + customerCode + " Is: " + xmlResponse);

                if ((!string.IsNullOrEmpty(xmlResponse)))
                {
                    apiMResponse.LoadXml(xmlResponse);
                    var node = apiMResponse.GetElementsByTagName("CustomerDownPay_Response");
                    apiMResponse = new XmlDocument();
                    apiMResponse.LoadXml(node[0].OuterXml);
                    XmlElement root = apiMResponse.DocumentElement;
                    string statusCode = root.ChildNodes[2].InnerText;
                    string statusMessage = root.ChildNodes[3].InnerText;
                    bool successCondition = statusMessage.Contains("successfully");

                    if (statusCode.ToString().ToUpper() == "S")
                    {
                     
                        if (successCondition)
                        {

                            CustomerImp ci = new CustomerImp();
                            ci.updateThirdPartyReference(transid, PayRef);

                            ci = null;
                            ErrHandler.Log(ClassMeth, customerCode, "Payment Update Response from UAC for " + customerCode + " is: " + xmlResponse);
                            ErrHandler.WriteError("Payment Update Response Received From UAC For " + customerCode + " Is: " + xmlResponse);
                            theReturner = "SUCCESS";
                        }
                        else
                        {
                            ErrHandler.Log(ClassMeth, refId, statusMessage);
                            ErrHandler.WriteError(" "+ statusMessage + "For " + customerCode + " and " + PayRef);
                            theReturner = statusMessage;
                        }
                    }
                    else if (statusCode.ToString().ToUpper() == "E")
                    {
                        string statusMessage2 = root.ChildNodes[3].InnerText;
                        ErrHandler.Log(ClassMeth, refId, statusMessage2);
                        ErrHandler.WriteError("Error while trying to update UAC reference for " + customerCode + " Is: " + statusMessage2);
                        theReturner = statusMessage2;
                    }
                    else
                    {
                        var errorNode = apiMResponse.GetElementsByTagName("faultstring");
                        apiMResponse.LoadXml(errorNode[0].OuterXml);
                        XmlElement errorRoot = apiMResponse.DocumentElement;
                        string errorMessage = errorRoot.ChildNodes[0].InnerText;
                        ErrHandler.Log(ClassMeth, refId, errorMessage);
                        ErrHandler.WriteError("Error while trying to update UAC reference for " + customerCode + " Is: " + errorMessage);
                        theReturner = errorMessage;
                    }
                }
                else
                {                    
                    ErrHandler.Log(ClassMeth, refId, "Empty Response from UAC");
                    ErrHandler.WriteError("Empty Response From UAC For " + customerCode + "");
                    theReturner = "Empty Response from BUA";
                }

            }
            catch (Exception ex)
            {
                ErrHandler.Log("UACPAY|UpdateFieldDetails", customerCode, ex.Message);
                ErrHandler.WriteError("Error validating partner NO: " + ex.Message.Replace("'", ""));
                theReturner = ex.Message;
            }
            
        }

        catch (Exception ex)
        {
            ErrHandler.Log(ClassMeth, "", ex.Message);
            theReturner = ex.Message;
        }
        return theReturner;
    }

    public string HttpPostUAC(string parameter, string url, string requestFormat, bool usenameVParm, objectMultiSelect nameVParam)
    {
        string classMethodName = "Utilities|HttpPostRestClient";

        try
        {
            var client = new RestClient(url);
            ServicePointManager.Expect100Continue = true;       /*code used for Could not create SSL / TLS secure channel*/
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;
            client.Authenticator = new HttpBasicAuthenticator(UACUserN, UACPassW);

            var request = new RestRequest(Method.POST);

            if (usenameVParm == false)
            {
                if (requestFormat.ToUpper() == "XML")
                {
                    //request.RequestFormat = DataFormat.Xml;
                    string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                               .GetBytes(UACUserN + ":" + UACPassW));
                    request.AddHeader("Authorization", "Basic " + encoded);
                    request.AddParameter("text/xml", parameter, ParameterType.RequestBody);
                    request.AddHeader("Accept", "text/xml");
                }
                else if (requestFormat.ToUpper() == "JSON")
                {
                    //request.RequestFormat = DataFormat.Json;
                    request.AddParameter("application/json", parameter, ParameterType.RequestBody);

                }
            }
            else
            {
                for (int i = 0; i < nameVParam.sItem.Length; i++)
                {
                    request.AddParameter(nameVParam.sItem[i], nameVParam.sValue[i]);
                    request.AddParameter("text/xml", nameVParam.sValue[i], ParameterType.RequestBody);
                }
            }

            var restResponse = client.Execute(request);

            if (string.IsNullOrEmpty(restResponse.Content))
            {
                ErrHandler.WriteError(" Response content is empty, Paramter is  " + parameter + " URL is " + url + " Error is " + restResponse.ErrorException + " Error Message is " + restResponse.ErrorException);
               
            }

            return restResponse.Content;
        }
        catch (Exception ex)
        {
            ErrHandler.Log(classMethodName, url, ex.Message);
            return "Get customer info failed.";
        }
    }
    
}