using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Support;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Logging
{
    internal class LatestLogEntriesCollector
    {
        private ConcurrentQueue<RequestLogEntry> LatestLogs { get; } = new ConcurrentQueue<RequestLogEntry>();
        private const int MAX_LATEST_LOG_ENTRIES = 1000;
        

        internal void TryAdd(IRequest request, object response, TimeSpan elapsed, RequestLoggerOptions opt = null)
        {
            if (LatestLogs.Count < LatestLogEntriesCollector.MAX_LATEST_LOG_ENTRIES || LatestLogs.TryDequeue(out RequestLogEntry entry))
                LatestLogs.Enqueue(CreateEntry(request, response, elapsed, opt));
        }

        internal List<RequestLogEntry> GetLatestLogs(int? take)
        {
            return LatestLogs
                .Take(take ?? int.MaxValue)
                .ToList();

        }

        private RequestLogEntry CreateEntry(IRequest request, object response, TimeSpan elapsed, RequestLoggerOptions opt = null)
        {
            var entry = new RequestLogEntry();

            entry.Id = request.GetId().ToString().ToInt64();
            entry.DateTime = DateTime.Now;
            entry.StatusCode = request.Response.StatusCode;
            entry.StatusDescription = request.Response.StatusDescription;
            entry.HttpMethod = request.Verb.ToUpper();
            entry.AbsoluteUri = request.AbsoluteUri;
            entry.PathInfo = request.PathInfo;
            entry.RequestBody = opt != null && opt.EnableRequestBodyTracking ? request.GetRawBody() : String.Empty;
            entry.RequestDto = request.Dto;
            entry.UserAuthId = request.GetSession()?.UserAuthId;
            entry.SessionId = request.GetSessionId();
            entry.IpAddress = request.UserHostAddress;
            entry.ForwardedFor = request.Headers[HttpHeaders.XForwardedFor];
            entry.Referer = request.Headers[HttpHeaders.Referer];
            entry.Headers = request.Headers.ToDictionary();
            entry.FormData = request.FormData.ToDictionary();
            entry.Items = request.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
            entry.Session = opt != null && opt.EnableSessionTracking ? request.GetSession() : null;
            entry.ResponseDto = request.GetResponseDto();

            entry.ErrorResponse = InMemoryRollingRequestLogger.ToSerializableErrorResponse(request.Response);
            if(response is Exception exception)
            {
                exception = exception.InnerException ?? exception;
                entry.ExceptionSource = exception.Source;
                entry.ExceptionData = exception.Data;
                
            }

            entry.RequestDuration = elapsed;

            return entry;
        }
    }
}
