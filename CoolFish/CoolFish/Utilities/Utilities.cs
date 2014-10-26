using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using CoolFishNS.Targets;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     A class with utilities that can be used.
    /// </summary>
    public static class Utilities
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static bool LoggersInitialized;

        private static readonly object LoggerLock = new object();

        /// <summary>
        ///     Gets the application path.
        ///     <value>The application path.</value>
        /// </summary>
        public static string ApplicationPath
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        /// <summary>
        ///     Gets the application version.
        /// </summary>
        public static Version Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().
                    GetName().Version;
            }
        }

        internal static string GetNews()
        {
            try
            {
                using (var client = new WebClient {Proxy = WebRequest.DefaultWebProxy})
                {
                    return client.DownloadString("http://unknowndev.github.io/CoolFish/Message.txt");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Could not connect to news feed. Website is down?", ex);
            }
            return string.Empty;
        }

        /// <summary>
        ///     Update values for a list of key-value pairs if the keys exist. Otherwise insert them into the dictionary
        /// </summary>
        /// <param name="dictionary">this IDictionary to upsert into</param>
        /// <param name="dictionaryToUpsert">the IDictionary of key-values to upsert</param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        public static void Upsert<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> dictionaryToUpsert)
        {
            foreach (var value in dictionaryToUpsert)
            {
                dictionary[value.Key] = value.Value;
            }
        }

        /// <summary>
        ///     Reconfigure all Logging Rules to log at the specified level and above only
        /// </summary>
        /// <param name="level">LogLevel to log at</param>
        public static void Reconfigure(LogLevel level)
        {
            LogManager.GlobalThreshold = level;
            Logger.Log(level, "Logging at the [" + level.Name.ToUpper() + "] level and above only");
        }

        /// <summary>
        ///     Reconfigure all Logging Rules to log at the specified level and above only
        /// </summary>
        /// <param name="ordinal">ordinal value of the LogLevel to log at</param>
        public static void Reconfigure(int ordinal)
        {
            Reconfigure(LogLevel.FromOrdinal(ordinal));
        }

        internal static void InitializeLoggers()
        {
            lock (LoggerLock)
            {
                if (LoggersInitialized)
                {
                    return;
                }
                var config = new LoggingConfiguration();

                DateTime now = DateTime.Now;

                string activeLogFileName = string.Format("{0}\\Logs\\{1}\\[CoolFish-{2}] {3}.txt", ApplicationPath,
                    now.ToString("MMMM dd yyyy"), Process.GetCurrentProcess().Id,
                    now.ToString("T").Replace(':', '.'));


                var file = new FileTarget
                {
                    FileName = activeLogFileName,
                    Layout =
                        @"[${date:format=MM/dd/yy h\:mm\:ss.ffff tt}] [${level:uppercase=true}] ${message} ${onexception:inner=${newline}${exception:format=tostring}}",
                    CreateDirs = true,
                    ConcurrentWrites = false
                };


                config.LoggingRules.Add(new LoggingRule("*", LogLevel.FromOrdinal(UserPreferences.Default.LogLevel),
                    new AsyncTargetWrapper(file) {OverflowAction = AsyncTargetWrapperOverflowAction.Grow}));


                var markedUp = new MarkedUpTarget
                {
                    Layout =
                        @"[${date:format=MM/dd/yy h\:mm\:ss.ffff tt}] [${level:uppercase=true}] ${message} ${onexception:inner=${newline}${exception:format=tostring}}"
                };


                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Error,
                    new AsyncTargetWrapper(markedUp) {OverflowAction = AsyncTargetWrapperOverflowAction.Grow}));


                LogManager.Configuration = config;
                LoggersInitialized = true;
            }
        }
    }
}