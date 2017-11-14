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
        public Type[] HideRequestBodyForRequestDtoTypes { get => Options.HideRequestBodyForRequestDtoTypes;
            set => Options.HideRequestBodyForRequestDtoTypes = value; }


        public LogEntryPropertiesGenerator LogEntryPropertiesGenerator { get => Options.LogEntryPropertiesGenerator;
            set => Options.LogEntryPropertiesGenerator = value; }


        public List<RequestLogEntry> GetLatestLogs(int? take)
        {
            return LatestLogEntriesCollector.GetLatestLogs(take);
        }

        public void Log(IRequest request, object requestDto, object response, TimeSpan elapsed)
        {
            if(!AssertCanLog(request, requestDto))
                return;

            RequestLoggerOptions loggingOptions = Options.Clone() as RequestLoggerOptions;

            if (request.Items.ContainsKey(Plugin.SerilogRequestLogsFeature.SerilogRequestLogsLoggerKey))
            {

                var logEvent = LogEventFactory
                    .Create(request, requestDto, response, elapsed, loggingOptions);

                if(logEvent != null)
                {
                    var logger = request.Items[Plugin.SerilogRequestLogsFeature.SerilogRequestLogsLoggerKey] as ILogger;

                    logger
                        .ForContext<IRequestLogger>()
                        .Write(logEvent);
                }

                LatestLogEntriesCollector
                    .Log(request, requestDto, response, elapsed);
            }
        }

        private bool AssertCanLog(IRequest request, object requestDto) => ShouldNotLog(request, requestDto).All(r => r == false);

        private IEnumerable<bool> ShouldNotLog(IRequest request, object requestDto)
        {
            yield return request == null;

            if (SkipLogging != null)
                yield return SkipLogging.Invoke(request);

            if (LimitToServiceRequests)
                yield return (requestDto ?? request?.Dto) == null;

            if (RequiredRoles != null && RequiredRoles.Any())
                yield return RequiredRoles.Except(request?.GetSession()?.Roles).Any() == false;

            if (LimitToServiceRequests && ExcludeRequestDtoTypes != null && ExcludeRequestDtoTypes.Any())
                yield return (requestDto ?? request.Dto) == null || ExcludeRequestDtoTypes.Contains((requestDto ?? request.Dto).GetType());

            yield break;

        }
    }
}
