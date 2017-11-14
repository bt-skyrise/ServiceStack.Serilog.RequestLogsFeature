using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Plugin
{
    [Exclude(ServiceStack.Feature.Soap)]
    [DataContract]
    public class SerilogRequestLogs
    {
        [DataMember(Order = 1)] public bool? EnableSessionTracking { get; set; }
        [DataMember(Order = 2)] public bool? EnableRequestBodyTracking { get; set; }
        [DataMember(Order = 3)] public bool? EnableResponseTracking { get; set; }
        [DataMember(Order = 4)] public bool? EnableErrorTracking { get; set; }
        [DataMember(Order = 5)] public bool? LimitToServiceRequests { get; set; }
        [DataMember(Order = 6)] public int Skip { get; set; }
        [DataMember(Order = 7)] public int? Take { get; set; }
    }

    [Exclude(ServiceStack.Feature.Soap)]
    [DataContract]
    public class SerilogRequestLogsResponse
    {
        public SerilogRequestLogsResponse()
        {
            this.Results = new List<RequestLogEntry>();
        }

        [DataMember(Order = 1)] public List<RequestLogEntry> Results { get; set; }
        [DataMember(Order = 2)] public Dictionary<string, string> Usage { get; set; }
        [DataMember(Order = 3)] public ResponseStatus ResponseStatus { get; set; }
    }

    [DefaultRequest(typeof(SerilogRequestLogs))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class FeatureService : Service
    {
        private static readonly Dictionary<string, string> Usage = new Dictionary<string, string> {
            ["bool EnableSessionTracking"] = "Turn On/Off Tracking of Session",
            ["bool EnableRequestBodyTracking"] = "Turn On/Off Tracking of Request Body",
            ["bool EnableResponseTracking"] = "Turn On/Off Tracking of Responses",
            ["bool EnableErrorTracking"] = "Turn On/Off Tracking of Errors",
            ["bool LimitToServiceRequests"] = "Turn On/Off Limiting of Service Requests",
            ["int Skip"] = "Skip past N results",
            ["int Take"] = "Only look at last N results"
        };

        public IRequestLogger RequestLogger { get; set; }

        public SerilogRequestLogsResponse Any(SerilogRequestLogs request)
        {
            if (RequestLogger == null)
                throw new Exception("No IRequestLogger is registered");

            if (!HostContext.DebugMode)
                RequiredRoleAttribute.AssertRequiredRoles(Request, RequestLogger.RequiredRoles);

            if (request.EnableSessionTracking.HasValue)
                RequestLogger.EnableSessionTracking = request.EnableSessionTracking.Value;

            if (request.EnableRequestBodyTracking.HasValue)
                RequestLogger.EnableRequestBodyTracking = request.EnableRequestBodyTracking.Value;

            if (request.LimitToServiceRequests.HasValue)
                RequestLogger.LimitToServiceRequests = request.LimitToServiceRequests.Value;

            if (request.EnableResponseTracking.HasValue)
                RequestLogger.EnableResponseTracking = request.EnableResponseTracking.Value;

            if (request.EnableErrorTracking.HasValue)
                RequestLogger.EnableErrorTracking = request.EnableErrorTracking.Value;

            var logs = RequestLogger
                .GetLatestLogs(request.Take)
                .Skip(request.Skip)
                .OrderByDescending(x => x.Id)
                .ToList();

            return new SerilogRequestLogsResponse
            {
                Results = logs,
                Usage = Usage
            };
        }
    }
}
