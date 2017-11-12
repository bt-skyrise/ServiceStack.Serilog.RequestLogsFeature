using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Logging
{
    internal class LogEventFactory
    {
        private static readonly string Property_HttpMethod_Key = "Method";
        private static readonly string Property_Url_Key = "Url";
        private static readonly string Property_StatusCode_Key = "Status";
        private static readonly string Property_Headers_Key = "Headers";
        private static readonly string Property_Body_Key = "Body";
        private static readonly string Property_Form_Key = "Form";

        private static readonly MessageTemplate LogEventMessageTemplate = 
            new MessageTemplateParser().Parse($@"HTTP {{{Property_HttpMethod_Key}}} {{{Property_Url_Key}}} responded {{{Property_StatusCode_Key}}}");

        internal LogEvent Create(IRequest request, object dto, RequestLoggerOptions opt) =>
            new LogEvent(
                timestamp: DateTimeOffset.Now,
                exception: null,
                level: LogEventLevel.Information,
                messageTemplate: LogEventFactory.LogEventMessageTemplate,
                properties: GetProperties(request, dto, opt)
            );

        private static IEnumerable<LogEventProperty> GetProperties(IRequest request, object dto, RequestLoggerOptions opt)
        {
            if(request != null)
            {
                yield return WithHttpMethod(request);
                yield return WithUrl(request);
                yield return WithStatusCode(request);
                yield return WithHeaders(request);
            }

            Type requestDtoType = dto.GetType();
            if(request != null 
                && 
                (
                    opt.HideRequestBodyForRequestDtoTypes == null
                    ||
                    opt.HideRequestBodyForRequestDtoTypes.All(@type => @type != requestDtoType)
                )
                &&
                opt.EnableRequestBodyTracking
            )
            {
                yield return WithRequestBody(request);
                yield return WithFormData(request);
            }
            
            if(request != null
                &&
                request.IsErrorResponse()
                &&
                opt.EnableErrorTracking
                )
            {
            }
            
            yield break;
        }

        private static LogEventProperty WithHttpMethod(IRequest request) 
            => new LogEventProperty($"{Property_HttpMethod_Key}", new ScalarValue(request.Verb.ToUpper()));

        private static LogEventProperty WithUrl(IRequest request) 
            => new LogEventProperty($"{Property_Url_Key}", new ScalarValue(request.PathInfo));

        private static LogEventProperty WithStatusCode(IRequest request) 
            => new LogEventProperty($"{Property_StatusCode_Key}", new ScalarValue(request.Response.StatusCode));

        private static LogEventProperty WithHeaders(IRequest request)
        {
            var headersAsLogEventProps = request
                .Headers
                .ToDictionary()
                .Select(dictItem => new LogEventProperty(dictItem.Key, new ScalarValue(dictItem.Value)))
                ;

            return new LogEventProperty($"{Property_Headers_Key}", new StructureValue(headersAsLogEventProps));
        }

        private static LogEventProperty WithRequestBody(IRequest request)
            => new LogEventProperty($"{Property_Body_Key}", new ScalarValue(request?.GetRawBody() ?? String.Empty));

        private static LogEventProperty WithFormData(IRequest request)
        {
            var formDataAsLogEventProps = request
                    .FormData
                    .ToDictionary()
                    .Select(dictItem => new LogEventProperty(dictItem.Key, new ScalarValue(dictItem.Value)))
                    ;

            return new LogEventProperty($"{Property_Form_Key}", new StructureValue(formDataAsLogEventProps));
        }


    }
}
