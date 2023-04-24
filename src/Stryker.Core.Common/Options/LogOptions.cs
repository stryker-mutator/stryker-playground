using Serilog.Events;

namespace Stryker.Core.Common.Options
{
    public class LogOptions
    {
        public bool LogToFile { get; init; }
        public LogEventLevel LogLevel { get; init; }
    }
}