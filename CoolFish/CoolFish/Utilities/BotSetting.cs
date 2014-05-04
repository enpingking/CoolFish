using System;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     A generic class for saving bot settings
    /// </summary>
    [Serializable]
    public class BotSetting
    {
        /// <summary>
        ///     The value of the bot setting
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        ///     Cast the setting to a specified type
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <returns>The BotSetting with the generic type passed</returns>
        public T As<T>()
        {
            return (T) Value;
        }

        /// <summary>
        ///     Creates a new instance of a setting with the passed value
        /// </summary>
        /// <param name="value">The value to assign</param>
        /// <returns>A new BotSetting with the passed value</returns>
        public static BotSetting As(object value)
        {
            return new BotSetting {Value = value};
        }

        public static implicit operator bool(BotSetting s)
        {
            return s.As<bool>();
        }

        public static implicit operator bool?(BotSetting s)
        {
            return s.As<bool?>();
        }

        public static implicit operator int(BotSetting s)
        {
            return s.As<int>();
        }

        public static implicit operator double(BotSetting s)
        {
            return s.As<double>();
        }

        public static implicit operator BotSetting(bool? s)
        {
            return As(s);
        }

        public static implicit operator BotSetting(int s)
        {
            return As(s);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}