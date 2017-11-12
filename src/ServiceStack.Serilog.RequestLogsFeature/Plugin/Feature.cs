using System;
using System.Threading.Tasks;
using Serilog;
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
            request.Items.Remove(SerilogRequestLogsLoggerKey);
        }

        private ILogger CreateSerilogLogger()
        {
            return SerilogLoggerFactory != null
                ? SerilogLoggerFactory.Invoke()
                : Log.Logger
                ;
        }
    }
}
