using NLog;

namespace CoolFishNS.Utilities
{
    public static class Logging
    {
        /// <summary>
        ///     NLog Logger instance for logging within CoolFish.
        ///     Useful for plugins to call this instead of using their own logging implementation
        /// </summary>
        public static Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     NLog LogManager access for logging within CoolFish
        ///     Useful for plugins to add their own Targets
        ///     Reference the NLog.dll to access built-in targets and class resolving
        /// </summary>
        public static LogManager LogManager = LogManager;
    }
}