using System;
using System.IO;
using System.Reflection;

namespace CoolFish
{
    /// <summary>
    ///     Defined constants that should remain the same always
    /// </summary>
    public static class Constants
    {
        /// <summary>
        ///     File name for where user preferences are saved
        /// </summary>
        public const string UserPreferencesFileName = "UserPreferences.dat";

        /// <summary>
        ///     The path to the currently executing process
        /// </summary>
        public static readonly Lazy<string> ApplicationPath = new Lazy<string>(() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        /// <summary>
        ///     The version of the currently executing process
        /// </summary>
        public static readonly Lazy<string> Version = new Lazy<string>(() => Assembly.GetExecutingAssembly().GetName().Version.ToString());
    }
}