using System.Collections.Generic;

namespace ServiceStack.Serilog.RequestLogsFeature.Plugin
{
    public class SerilogRequestLogs
    {
    }

    public class SerilogRequestLogsResponse
    {
        public SerilogRequestLogsResponse()
        {
            this.Results = new List<RequestLogEntry>();
        }

        public List<RequestLogEntry> Results { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DefaultRequest(typeof(SerilogRequestLogs))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class FeatureService : Service
    {
    }
}
