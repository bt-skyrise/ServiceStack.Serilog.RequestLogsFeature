using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using ServiceStack.Serilog.RequestLogsFeature.Logging;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Plugin
{
    public class SerilogRequestLogsFeature : IPlugin
    {
        private readonly FeatureValidator Validator = new FeatureValidator();

        public const string SerilogRequestLogsLoggerKey = "SerilogRequestLogs.Logger";

        /// <summary>
        /// SerilogRequestLogs service Route, default is /serilogrequestlogs
        /// </summary>
        public string AtRestPath { get; set; }

        /// <summary>
        /// Request Logger instance
        /// </summary>
        public IRequestLogger RequestLogger { get; set; }

        /// <summary>
        /// Delegate used to construct custom serilog logger instance
        /// </summary>
        public Func<ILogger> SerilogLoggerFactory { get; set; }

        /// <summary>
        /// Delegate used to configure local instance of Serilog logger.
        /// It should not be used with <see cref="SerilogLoggerFactory"/> delegate.
        /// </summary>
        public Func<LoggerConfiguration, LoggerConfiguration> SerilogLoggerBuilder { get; set; }

        /// <summary>
        /// Delegate used to provide collection of properties included in log entry. 
        /// </summary>
        public LogEntryPropertiesGenerator LogEntryPropertiesGenerator{ get; set; }

        /// <summary>
        /// Collection of roles that user should have for logging.
        /// </summary>
        public string[] RequiredRoles { get; set; }

        /// <summary>
        /// Delegate used to check if log entry should be created.
        /// </summary>
        public Func<IRequest, bool> SkipLogging { get; set; }

        /// <summary>
        /// Collection of request dtos' types that shouldn't be logged.
        /// </summary>
        public Type[] ExcludeRequestDtoTypes { get; set; }

        /// <summary>
        /// Collection of request dtos' types that should not be included in log entry.
        /// </summary>
        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

        public SerilogRequestLogsFeature()
        {
            AtRestPath = "/serilogrequestlogs";
        }

        public void Register(IAppHost appHost)
        {
            //Validator.ValidateAndThrow(this);

            appHost.GlobalRequestFiltersAsync.Add(RequestFilter);
            appHost.GlobalResponseFilters.Add(ResponseFilter);
            
            var requestLogger = new RequestLogger();
            requestLogger = new FeatureConfig().ApplyAppSettings(requestLogger, appHost);
            requestLogger.LogEntryPropertiesGenerator = LogEntryPropertiesGenerator;
            requestLogger.RequiredRoles = RequiredRoles;
            requestLogger.SkipLogging = SkipLogging;
            requestLogger.ExcludeRequestDtoTypes = ExcludeRequestDtoTypes;
            requestLogger.HideRequestBodyForRequestDtoTypes = HideRequestBodyForRequestDtoTypes;
            appHost.Register<IRequestLogger>(requestLogger);

            appHost.RegisterService<FeatureService>(AtRestPath);

            appHost.GetPlugin<MetadataFeature>()
                .AddDebugLink(AtRestPath, "Serilog Request Logs");
        }

        private Task RequestFilter(IRequest request, IResponse response, object dto)
        {
            if (request.Items.ContainsKey(SerilogRequestLogsLoggerKey))
                request.Items.Remove(SerilogRequestLogsLoggerKey);

            request.Items.Add(SerilogRequestLogsLoggerKey, CreateSerilogLogger());

            return Task.CompletedTask;
        }

        private void ResponseFilter(IRequest request, IResponse response, object dto)
        {
            if (request.Items.ContainsKey(SerilogRequestLogsLoggerKey))
            {
                var logger = request.Items[SerilogRequestLogsLoggerKey] as IDisposable;
                logger?.Dispose();

                request.Items.Remove(SerilogRequestLogsLoggerKey);
            }
        }

        private ILogger CreateSerilogLogger()
        {
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Information();

            if (SerilogLoggerBuilder != null)
                loggerConfiguration = SerilogLoggerBuilder(loggerConfiguration);

            return SerilogLoggerFactory != null
                ? SerilogLoggerFactory.Invoke()
                : loggerConfiguration.CreateLogger()
                ;
        }
    }
}
