using System;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Logging
{
    internal class RequestLoggerOptions : ICloneable
    {
        public bool EnableSessionTracking { get; set; }
        public bool EnableRequestBodyTracking { get; set; }
        public bool EnableResponseTracking { get; set; }
        public bool EnableErrorTracking { get; set; }
        public bool LimitToServiceRequests { get; set; }
        public string[] RequiredRoles { get; set; }
        public Func<IRequest, bool> SkipLogging { get; set; }
        public Type[] ExcludeRequestDtoTypes { get; set; }
        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }
        
        internal RequestLoggerOptions GetCopy()
        {
            return (RequestLoggerOptions)MemberwiseClone();
        }

        public object Clone()
        {
            return GetCopy();
        }
    }
}
