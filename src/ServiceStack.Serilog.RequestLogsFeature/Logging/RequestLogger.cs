using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Logging
{
    public class RequestLogger : IRequestLogger
    {
        private LogEventFactory LogEventFactory{ get; } = new LogEventFactory();
        private InMemoryRollingRequestLogger LatestLogEntriesCollector { get; } = new InMemoryRollingRequestLogger(capacity:1000);
        private RequestLoggerOptions Options { get; set; } = new RequestLoggerOptions();

        public bool EnableSessionTracking { get => Options.EnableSessionTracking;
            set => LatestLogEntriesCollector.EnableSessionTracking = Options.EnableSessionTracking = value; }
        public bool EnableRequestBodyTracking { get => Options.EnableRequestBodyTracking;
            set => LatestLogEntriesCollector.EnableRequestBodyTracking = Options.EnableRequestBodyTracking = value; }
        public bool EnableResponseTracking { get => Options.EnableResponseTracking;
            set => LatestLogEntriesCollector.EnableResponseTracking = Options.EnableResponseTracking = value; }
        public bool EnableErrorTracking { get => Options.EnableErrorTracking;
            set => LatestLogEntriesCollector.EnableErrorTracking = Options.EnableErrorTracking = value; }
        public bool LimitToServiceRequests { get => Options.LimitToServiceRequests;
            set => LatestLogEntriesCollector.LimitToServiceRequests = Options.LimitToServiceRequests = value; }
        public string[] RequiredRoles { get => Options.RequiredRoles;
            set => LatestLogEntriesCollector.RequiredRoles = Options.RequiredRoles = value; }
        public Func<IRequest, bool> SkipLogging { get => Options.SkipLogging;
            set => LatestLogEntriesCollector.SkipLogging = Options.SkipLogging = value; }
        public Type[] ExcludeRequestDtoTypes { get => Options.ExcludeRequestDtoTypes;
            set => LatestLogEntriesCollector.ExcludeRequestDtoTypes = Options.ExcludeRequestDtoTypes = value; }
        public Type[] HideRequestBodyForRequestDtoTypes { get => Options.HideRequestBodyForRequestDtoTypes; set => Options.HideRequestBodyForRequestDtoTypes = value; }

        public List<RequestLogEntry> GetLatestLogs(int? take)
        {
            return LatestLogEntriesCollector.GetLatestLogs(take);
        }

        public void Log(IRequest request, object requestDto, object response, TimeSpan elapsed)
        {
            RequestLoggerOptions loggingOptions = Options.Clone() as RequestLoggerOptions;

            if (request.Items.ContainsKey(Plugin.Feature.SerilogRequestLogsLoggerKey))
            {
                var logger = request.Items[Plugin.Feature.SerilogRequestLogsLoggerKey] as ILogger;

                var logEvent = LogEventFactory
                    .Create(request, loggingOptions);

                logger
                    .ForContext<IRequestLogger>()
                    .Write(logEvent);

                LatestLogEntriesCollector
                    .Log(request, requestDto, response, elapsed);
            }
        }
    }
}
