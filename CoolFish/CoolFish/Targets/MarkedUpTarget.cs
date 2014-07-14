using CoolFishNS.Analytics;
using MarkedUp;
using NLog;
using NLog.Common;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace CoolFishNS.Targets
{
    public class MarkedUpTarget : TargetWithLayout
    {
        public MarkedUpTarget()
        {
            if (!MarkedUpAnalytics.IsInitialized)
            {
                MarkedUpAnalytics.Initialize();
            }
        }

        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            foreach (AsyncLogEventInfo logEvent in logEvents)
            {
                Write(logEvent);
            }
        }

        protected override void Write(AsyncLogEventInfo logEvent)
        {
            Write(logEvent.LogEvent);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent.Level == LogLevel.Error && logEvent.Exception != null)
            {
                AnalyticClient.Error(logEvent.FormattedMessage, logEvent.Exception);
            }
            else if (logEvent.Level == LogLevel.Fatal && logEvent.Exception != null)
            {
                AnalyticClient.Fatal(logEvent.FormattedMessage, logEvent.Exception);
            }
        }
    }
}