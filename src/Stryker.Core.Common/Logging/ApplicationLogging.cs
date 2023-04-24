using Microsoft.Extensions.Logging;

namespace Stryker.Core.Common.Logging;

public static class ApplicationLogging
{
    public static ILoggerFactory LoggerFactory { get; set; }
}