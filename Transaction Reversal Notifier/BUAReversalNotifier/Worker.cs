using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MbokoReversalNotifier.Processes;
using MbokoReversalNotifier.Interfaces;
using MbokoReversalNotifier.Helpers.ConfigurationSettinigs.AppSettings;
using MbokoReversalNotifier.Helpers.ConfigurationSettinigs;

namespace MbokoReversalNotifier
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;        
        private readonly ISqlProcess _sqlProcess;
        private readonly MbokoNotifier _userRep;
        //private readonly webConfigAttributes _config;
        public static IConfiguration _configs;

        public Worker(ILogger<Worker> logger, ISqlProcess sqlProcess, MbokoNotifier userRep, IConfiguration configs) 
        => (_logger, _sqlProcess, _userRep, _configs, ConfigurationSettingsHelper.Configuration) = (logger, sqlProcess, userRep, configs, configs);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker running at: {DateTime.Now}");
                var result = await _userRep.GetTransactions();
                if ((result == "ERROR") || (result == "SUCCESS"))
                {
                    await Task.Delay(ConfigSettings.webConfigAttributes.jobDelay, stoppingToken);
                }
            }
        }
    }
}
