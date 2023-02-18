using System;
using System.Collections.Generic;
using System.Text;
using MbokoReversalNotifier.Interfaces;
using MbokoReversalNotifier.Helpers.Models;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace MbokoReversalNotifier.Repositories
{
    public class ProtocolHandler : IProtocolHandler
    {
        private readonly ILogger<sqlProcess> _logger;
        public ProtocolHandler(ILogger<sqlProcess> logger) => (_logger) = (logger);
        public async Task<string> HttpPostMethod(string destination, string token, string parameter, string url, bool usenameVParm, objectMultiSelect nameVParam)
        {
            string classMethodName = "Utilities|HttpPostRestClient";

            try
            {
                var client = new RestClient(url);
             
                var request = new RestRequest(Method.POST);
                //var request = new RestRequest("/", Method.Post);

                if (usenameVParm == false)
                {
                    request.AddHeader("content-type", "application/json");
                    request.AddHeader("x-api-key", token);
                    request.AddParameter("application/json", parameter, ParameterType.RequestBody);
                }
                else
                {
                    for (int i = 0; i < nameVParam.sItem.Length; i++)
                    {
                        request.AddParameter(nameVParam.sItem[i], nameVParam.sValue[i]);
                    }
                }

                //var restResponse = await client.ExecuteAsync(request, null);
                var restResponse = client.Execute(request);
                if (string.IsNullOrEmpty(restResponse.Content))
                {
                    _logger.LogInformation(" Response content is empty, Parameter is  " + parameter + " URL is " + url + " Error is " + restResponse.ErrorException + " Error Message is " + restResponse.ErrorException);
                }

                return restResponse.Content;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{classMethodName}, {ex.Message}, {ex.StackTrace}");
                return "Get customer info failed.";
            }
        }
    }
}
