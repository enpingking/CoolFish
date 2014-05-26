using System.Globalization;
using System.Threading;
using CoolFishNS.Utilities;
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
            if (!Analytics.MarkedUp.IsInitialized)
            {
                Analytics.MarkedUp.Initialize();
            }
        }

        private void SetThreadCulture()
        {
            // Change culture under which this application runs
            var ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
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
            if (logEvent.Level == LogLevel.Error)
            {
                SetThreadCulture();
                AnalyticClient.Error(logEvent.FormattedMessage, logEvent.Exception);
            }
            else if (logEvent.Level == LogLevel.Fatal)
            {
                SetThreadCulture();
                AnalyticClient.Fatal(logEvent.FormattedMessage, logEvent.Exception);
            }
        }
    }
}