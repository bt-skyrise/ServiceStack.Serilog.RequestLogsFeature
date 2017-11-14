using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Logging
{
    internal class LogEventFactory
    {
        private static readonly string Property_HttpMethod_Key = "Method";
        private static readonly string Property_Url_Key = "Url";
        private static readonly string Property_StatusCode_Key = "StatusCode";
        private static readonly string Property_StatusDesc_Key = "StatusDescription";
        private static readonly string Property_Elapsed_Key = "Elasped";
        private static readonly string Property_Headers_Key = "Headers";
        private static readonly string Property_Body_Key = "Body";
        private static readonly string Property_Form_Key = "Form";
        private static readonly string Property_ReqDto_Key = "RequestDto";
        private static readonly string Property_Session_Key = "Session";
        private static readonly string Property_Response_Key = "Response";
        private static readonly string Property_Error_Key = "Error";

        private static readonly MessageTemplate LogEventMessageTemplate = 
            new MessageTemplateParser().Parse($@"HTTP {{{Property_HttpMethod_Key}}} {{{Property_Url_Key}}} responded {{{Property_StatusCode_Key}}} in {{{Property_Elapsed_Key}}} ms");

        internal LogEvent Create(IRequest request, object requestDto, object responseDto, TimeSpan elapsed, RequestLoggerOptions opt)
            => new LogEvent(
                timestamp: DateTimeOffset.Now,
                exception: null,
                level: LogEventLevel.Information,
                messageTemplate: LogEventFactory.LogEventMessageTemplate,
                properties: GetProperties(request, requestDto, responseDto, elapsed, opt)
            );

        private static IEnumerable<LogEventProperty> GetProperties(IRequest request, object requestDto, object responseDto, TimeSpan elapsed, RequestLoggerOptions opt)
        {
            yield return WithHttpMethod(request);
            yield return WithUrl(request);
            yield return WithHeaders(request);
            yield return WithStatusCode(request);
            yield return WithStatusDescription(request);
            yield return WithElapsedTime(elapsed);


            bool IsRequestDtoExcludedFromLogging(Type dtoType) => opt.HideRequestBodyForRequestDtoTypes != null && opt.HideRequestBodyForRequestDtoTypes.Any(@type => @type != dtoType);
            Type requestDtoType = (requestDto ?? request.Dto)?.GetType();
            if (requestDtoType != null 
                && !IsRequestDtoExcludedFromLogging(requestDtoType) 
                && opt.EnableRequestBodyTracking
                )
            {
                yield return WithRequestBody(request);
                yield return WithFormData(request);
                yield return WithRequestDto(request, requestDto);
            }


            if (opt.EnableSessionTracking) yield return WithSession(request);
            if (opt.EnableResponseTracking) yield return WithResponse(request, responseDto);

            if (request.IsErrorResponse() && opt.EnableErrorTracking)
            {
                var prop = WithErrorResponse(request, responseDto);
                if (prop != null) yield return prop;
            }

            foreach (var prop in WithPropertiesFromDelegate(request, requestDto, responseDto, opt.LogEntryPropertiesGenerator))
                yield return prop;


            yield break;
        }

        private static LogEventProperty WithHttpMethod(IRequest request) 
            => new LogEventProperty(Property_HttpMethod_Key, new ScalarValue(request.Verb.ToUpper()));

        private static LogEventProperty WithUrl(IRequest request) 
            => new LogEventProperty(Property_Url_Key, new ScalarValue(request.PathInfo));

        private static LogEventProperty WithStatusCode(IRequest request) 
            => new LogEventProperty(Property_StatusCode_Key, new ScalarValue(request.Response.StatusCode));

        private static LogEventProperty WithStatusDescription(IRequest request)
            => new LogEventProperty(Property_StatusDesc_Key, new ScalarValue(request.Response.StatusDescription));

        private static LogEventProperty WithHeaders(IRequest request)
        {
            var headersAsLogEventProps = request
                .Headers
                .ToDictionary()
                .Select(dictItem => new LogEventProperty(dictItem.Key, new ScalarValue(dictItem.Value)))
                ;

            return new LogEventProperty(Property_Headers_Key, new StructureValue(headersAsLogEventProps));
        }

        private static LogEventProperty WithRequestBody(IRequest request)
            => new LogEventProperty(Property_Body_Key, new ScalarValue(request?.GetRawBody() ?? String.Empty));

        private static LogEventProperty WithFormData(IRequest request){
            var formDataAsLogEventProps = request
                    .FormData
                    .ToDictionary()
                    .Select(dictItem => new LogEventProperty(dictItem.Key, new ScalarValue(dictItem.Value)))
                    ;

            return new LogEventProperty(Property_Form_Key, new StructureValue(formDataAsLogEventProps));
        }

        private static LogEventProperty WithRequestDto(IRequest request, object requestDto){
            var dto = requestDto ?? request.Dto;
            var logger = request.Items[Plugin.SerilogRequestLogsFeature.SerilogRequestLogsLoggerKey] as ILogger;
            logger.BindProperty(Property_ReqDto_Key, dto, true, out LogEventProperty logEventProperty);

            return logEventProperty;
        }

        private static LogEventProperty WithSession(IRequest request)
        {
            var session = request.GetSession();
            var logger = request.Items[Plugin.SerilogRequestLogsFeature.SerilogRequestLogsLoggerKey] as ILogger;
            logger.BindProperty(Property_Session_Key, session, true, out LogEventProperty logEventProperty);

            return logEventProperty;
        }

        private static LogEventProperty WithResponse(IRequest request, object responseDto)
        {
            var logger = request.Items[Plugin.SerilogRequestLogsFeature.SerilogRequestLogsLoggerKey] as ILogger;
            logger.BindProperty(Property_Response_Key, responseDto, true, out LogEventProperty logEventProperty);

            return logEventProperty;
        }

        private static LogEventProperty WithElapsedTime(TimeSpan elapsed)
            => new LogEventProperty(Property_Elapsed_Key, new ScalarValue(Math.Ceiling(elapsed.TotalMilliseconds)));

        private static LogEventProperty WithErrorResponse(IRequest request, object responseDto)
        {
            var logger = request.Items[Plugin.SerilogRequestLogsFeature.SerilogRequestLogsLoggerKey] as ILogger;
            

            if (responseDto is IHttpResult errorResult)
            {
                logger.BindProperty(Property_Error_Key, errorResult.Response, true, out LogEventProperty logEventProperty);
                return logEventProperty;
            }

            if(responseDto is Exception exception)
            {
                var responseStatus = (exception.InnerException ?? exception).ToResponseStatus();
                logger.BindProperty(Property_Error_Key, responseStatus, true, out LogEventProperty logEventProperty);
                return logEventProperty;
            }

            return null;
        }

        private static IEnumerable<LogEventProperty> WithPropertiesFromDelegate(IRequest request, object requestDto, object responseDto, LogEntryPropertiesGenerator propertiesGenerator)
            => propertiesGenerator != null ? propertiesGenerator.Invoke(request, requestDto, responseDto) : Enumerable.Empty<LogEventProperty>();

    }
}
