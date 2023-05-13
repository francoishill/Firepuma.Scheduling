using NLog;
using NLog.Config;
using NLog.Targets;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.NLogLogging;

public static class NLogLoggingExtensions
{
    public static LoggingConfiguration AddLocalLog4ViewerNLogTarget(
        this LoggingConfiguration loggingConfiguration)
    {
        var nLogViewerTarget = new NLogViewerTarget("nlogviewer")
        {
            Address = "udp://host.docker.internal:9999",
            Layout = "${longdate} ${callSite} ${logger} ${message} ${exception:format=tostring}",
        };

        loggingConfiguration.AddTarget(nLogViewerTarget);
        loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, nLogViewerTarget));

        loggingConfiguration.Reload();

        return loggingConfiguration;
    }
}