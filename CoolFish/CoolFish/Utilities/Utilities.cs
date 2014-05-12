using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NLog;
using NLog.Config;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     A class with utilities that can be used.
    /// </summary>
    public static class Utilities
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            foreach (LoggingRule rule in LogManager.Configuration.LoggingRules)
            {
                foreach (LogLevel logLevel in rule.Levels)
                {
                    rule.DisableLoggingForLevel(logLevel);
                }

                if (level == LogLevel.Off)
                {
                    // Do Nothing
                }
                else
                {
                    for (int i = level.Ordinal; i < LogLevel.Off.Ordinal; i++)
                    {
                        rule.EnableLoggingForLevel(LogLevel.FromOrdinal(i));
                    }
                }
            }

            LogManager.ReconfigExistingLoggers();

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
    }
}