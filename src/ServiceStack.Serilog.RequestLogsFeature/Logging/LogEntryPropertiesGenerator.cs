using System.Collections.Generic;
using Serilog.Events;
using ServiceStack.Web;

namespace ServiceStack.Serilog.RequestLogsFeature.Logging
{
    public delegate IEnumerable<LogEventProperty> LogEntryPropertiesGenerator(IRequest request, object requestDto, object responseDto);
}
