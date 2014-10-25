using CoolFishNS.Targets;
using MarkedUp;
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers;
using LogLevel = NLog.LogLevel;

namespace CoolFishNS.Analytics
{
    public static class MarkedUpAnalytics
    {
        public static bool IsInitialized
        {
            get
            {
#if DEBUG
                return true;
#endif
                return AnalyticClient.IsInitialized;
            }
        }

        public static void Initialize(string apiKey, string appName)
        {
#if DEBUG
            return;
#endif
            if (IsInitialized || apiKey == null || appName == null)
            {
                return;
            }
            AnalyticClient.Initialize(apiKey);
            AnalyticClient.AppStart();
            AnalyticClient.SessionStart();
            AnalyticClient.EnterPage(appName);

            var markedUp = new MarkedUpTarget();

            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Error,
                new AsyncTargetWrapper(markedUp) {OverflowAction = AsyncTargetWrapperOverflowAction.Grow}));
            LogManager.ReconfigExistingLoggers();
        }


        public static void ShutDown(string appName)
        {
#if DEBUG
            return;
#endif
            AnalyticClient.ExitPage(appName);
            AnalyticClient.SessionEnd();
            AnalyticClient.AppExit();
        }
    }
}