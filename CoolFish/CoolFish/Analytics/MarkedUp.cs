using System;
using System.Collections.Generic;
using MarkedUp;
using NLog;

namespace CoolFishNS.Analytics
{
    internal static class MarkedUp
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly DateTime StartTime = DateTime.Now;
        

        internal static bool IsInitialized
        {
            get
            {
#if DEBUG
                return true;
#endif
                return AnalyticClient.IsInitialized;
            }
        }

        internal static void Initialize()
        {
#if DEBUG
            return;
#endif
            if (IsInitialized)
            {
                return;
            }
            AnalyticClient.Initialize("e838c760-c238-41c3-b4bd-51640a8572dc", "");
            AnalyticClient.AppStart();
            AnalyticClient.SessionStart();
            AnalyticClient.EnterPage("CoolFish");
        }

        private static void TrackTime()
        {
#if DEBUG
            return;
#endif
            var dict = new Dictionary<string, string>();
            var span = (DateTime.Now - StartTime);
            
            Logger.Info("Duration: " + span.ToString("g"));
            
            if (span.TotalHours >= 99)
            {
                dict["days"] = Math.Round(span.TotalHours).ToString();
            }
            else if (span.TotalHours >= 1)
            {
                dict["hours"] = span.ToString(@"hh\:mm");
            }
            else if (span.TotalMinutes >= 1)
            {
                dict["minutes"] = span.ToString(@"mm\:ss");
            }
            else
            {
                dict["seconds"] = span.ToString(@"ss");
            }
            AnalyticClient.SessionEvent("UsageTime", dict);
        }

        internal static void ShutDown()
        {
#if DEBUG
            return;
#endif
            TrackTime();
            AnalyticClient.ExitPage("CoolFish");
            AnalyticClient.SessionEnd();
            AnalyticClient.AppExit();
        }
    }
}