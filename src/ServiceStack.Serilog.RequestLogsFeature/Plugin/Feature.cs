using System.Threading.Tasks;
using Serilog;
using ServiceStack.FluentValidation;
using ServiceStack.Logging.Serilog;
using ServiceStack.Serilog.RequestLogsFeature.Logging;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Plugin
{
    public class Feature : IPlugin
    {
        private readonly FeatureValidator Validator = new FeatureValidator();

        public const string SerilogRequestLogsLoggerKey = "SerilogRequestLogs.Logger";

        /// <summary>
        /// SerilogRequestLogs service Route, default is /serilogrequestlogs
        /// </summary>
        public string AtRestPath { get; set; }

        /// <summary>
        /// Serilog Logger instance
        /// </summary>
        public IRequestLogger RequestLogger { get; set; }

        public Feature()
        {
            AtRestPath = "/serilogrequestlogs";
        }

        public void Register(IAppHost appHost)
        {
            var requestLogger = new RequestLogger();

            //Validator.ValidateAndThrow(this);

            appHost.GlobalRequestFiltersAsync.Add(RequestFilter);
            appHost.GlobalResponseFilters.Add(ResponseFilter);

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
            
        }

        private ILogger CreateSerilogLogger()
        {
            return Log.Logger;
        }
    }
}
