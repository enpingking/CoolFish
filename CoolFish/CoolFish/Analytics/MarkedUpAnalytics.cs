using System;
using MarkedUp;
using NLog;

namespace CoolFishNS.Analytics
{
    internal static class MarkedUpAnalytics
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            AnalyticClient.Initialize("e838c760-c238-41c3-b4bd-51640a8572dc");
            AnalyticClient.AppStart();
            AnalyticClient.SessionStart();
            AnalyticClient.EnterPage("CoolFish");
        }


        internal static void ShutDown()
        {
#if DEBUG
            return;
#endif
            AnalyticClient.ExitPage("CoolFish");
            AnalyticClient.SessionEnd();
            AnalyticClient.AppExit();
        }
    }
}