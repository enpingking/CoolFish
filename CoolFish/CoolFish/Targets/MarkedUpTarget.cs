using System.Collections.Generic;
using System.Linq;
using MarkedUp;
using NLog;
using NLog.Common;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace CoolFishNS.Targets
{
    public class MarkedUpTarget : TargetWithLayout
    {
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
            if (logEvent.Exception != null)
            {
                Dictionary<string, string> dict = logEvent.Exception.Data.Keys.Cast<object>()
                    .ToDictionary(key => key.ToString(), key => logEvent.Exception.Data[key].ToString());
                dict.Add("TargetSite", logEvent.Exception.TargetSite.Name);
                dict.Add("Message", logEvent.Exception.Message);
                AnalyticClient.SessionEvent(logEvent.Exception.GetType().Name, dict);
            }
        }
    }
}