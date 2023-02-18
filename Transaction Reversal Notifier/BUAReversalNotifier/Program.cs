using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using AutoMapper;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.SystemConsole.Themes;
using MbokoReversalNotifier.Interfaces;
using MbokoReversalNotifier.Repositories;
using MbokoReversalNotifier.Helpers.Automapper;
using MbokoReversalNotifier.Processes;

namespace MbokoReversalNotifier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Literate, applyThemeToRedirectedOutput: true)
                .CreateLogger();
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch(Exception onBuild)
            {
                Log.Fatal(onBuild.StackTrace, $"Mboko Reversal Notifier failed initiation ... Details: {onBuild.Message}");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    //Connectionstrings Connectionstrings = configuration.GetSection("ConnectionStrings").Get<Connectionstrings>();
                    //webConfig webConfig = configuration.GetSection("webConfigAttributes").Get<webConfig>();
                    services.AddHostedService<Worker>();
                    services.AddOptions();
                    services.AddAutoMapper(typeof(mapperProfile));
                    services.AddTransient<ISqlProcess, sqlProcess>();
                    services.AddTransient<MbokoNotifier>();
                    services.AddTransient<IProtocolHandler, ProtocolHandler>();
                }).ConfigureLogging((hostContext, builder) =>
                {
                    builder.ConfigureSerilog(hostContext.Configuration);
                }).UseSerilog((hostingContext, LoggerConfiguration) => {
                    LoggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                });
    }

    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder ConfigureSerilog(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            return loggingBuilder;
        }
    }
}
