using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     A class with utilities that can be used.
    /// </summary>
    public static class Utilities
    {
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
    }
}