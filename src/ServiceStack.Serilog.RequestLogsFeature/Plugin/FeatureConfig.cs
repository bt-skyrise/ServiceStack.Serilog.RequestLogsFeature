using ServiceStack.Configuration;
using ServiceStack.Serilog.RequestLogsFeature.Logging;

namespace ServiceStack.Serilog.RequestLogsFeature.Plugin
{
    public class FeatureConfig
    {
        public const string EnableSessionTrackingKey = "serilogrequestlogs:EnableSessionTracking";
        public const string EnableRequestBodyTrackingKey = "serilogrequestlogs:EnableRequestBodyTracking";
        public const string EnableResponseTrackingKey = "serilogrequestlogs:EnableResponseTracking";
        public const string EnableErrorTrackingKey = "serilogrequestlogs:EnableErrorTracking";
        public const string LimitToServiceRequestsKey = "serilogrequestlogs:LimitToServiceRequests";
        
        public RequestLogger ApplyAppSettings(RequestLogger logger, IAppHost appHost)
        {
            if (logger == null)
                return logger;

            var appSettings = new AppSettings();
            logger.EnableSessionTracking = appSettings.Get<bool>(EnableSessionTrackingKey);
            logger.EnableRequestBodyTracking = appSettings.Get<bool>(EnableRequestBodyTrackingKey);
            logger.EnableResponseTracking = appSettings.Get<bool>(EnableResponseTrackingKey);
            logger.EnableErrorTracking = appSettings.Get<bool>(EnableErrorTrackingKey);
            logger.LimitToServiceRequests = appSettings.Get<bool>(LimitToServiceRequestsKey);

            return logger;
        }
    }
}
