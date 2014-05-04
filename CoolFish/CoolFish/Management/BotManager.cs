using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CoolFishNS.Bots;
using CoolFishNS.Bots.CoolFishBot;
using CoolFishNS.Management.CoolManager;
using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.PluginSystem;
using CoolFishNS.Utilities;
using GreyMagic;

namespace CoolFishNS.Management
{
    /// <summary>
    ///     Bot manager class for performing operations on IBot implemented classes
    /// </summary>
    public static class BotManager
    {
        internal static readonly Dictionary<string, IBot> LoadedBots = new Dictionary<string, IBot>();

        static BotManager()
        {
            var bot = new CoolFishBot();
            LoadBot(bot);
            SetActiveBot(bot);
        }

        /// <summary>
        ///     The main ExternalProcessReader object that reads/writes memory to the attached process
        /// </summary>
        public static ExternalProcessReader Memory { get; private set; }

        /// <summary>
        ///     Returns true if we are attached to a Wow process and can perform memory operations and DXHook methods
        /// </summary>
        public static bool IsAttached
        {
            get { return Memory != null && Memory.IsProcessOpen && DxHook.Instance.IsApplied; }
        }

        /// <summary>
        ///     Currently active IBot object that the user interface will interact with.
        ///     This field should be set to whatever Bot object you want to respond to UI functions (Start, Stop, etc.)
        /// </summary>
        public static IBot ActiveBot { get; private set; }

        /// <summary>
        ///     Get the currently logged in toon's name
        /// </summary>
        /// <returns>string of the player's name</returns>
        public static string GetToonName
        {
            get
            {
                try
                {
                    return Memory.ReadString(Offsets.Addresses["PlayerName"], Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Logging.Log(ex);
                    return string.Empty;
                }
            }
        }

        /// <summary>
        ///     Gets a value indicating whether logged into the game and on a player character.
        /// </summary>
        /// <value>
        ///     <c>true</c> if logged in; otherwise, <c>false</c>.
        /// </value>
        public static bool LoggedIn
        {
            get
            {
                try
                {
                    return Memory.Read<uint>(Offsets.Addresses["LoadingScreen"]) == 1;
                }
                catch (Exception ex)
                {
                    Logging.Log(ex); // if we can't read it, we're probably logged out
                    return false;
                }
            }
        }

        /// <summary>
        ///     Loads an IBot implementing class into CoolFish's BotManager for display and use from the interface
        /// </summary>
        /// <param name="botToLoad">IBot implementing instance to load into CoolFish</param>
        public static void LoadBot(IBot botToLoad)
        {
            string id = GetBotId(botToLoad);
            if (!IsBotLoaded(botToLoad))
            {
                Logging.Write("Loaded " + id);
                LoadedBots[id] = botToLoad;
            }
            else
            {
                Logging.Write("Bot " + id + " has already been loaded. Skipping load...");
            }
        }

        /// <summary>
        ///     Returns whether or not a bot with a particular unique keyId has been loaded or not
        /// </summary>
        /// <param name="bot">IBot to look up</param>
        /// <returns>true if the bot is already loaded; otherwise, false</returns>
        public static bool IsBotLoaded(IBot bot)
        {
            return LoadedBots.ContainsKey(GetBotId(bot));
        }

        /// <summary>
        ///     Get the unique identifier string the BotManager uses to compare IBot classes.
        ///     Currently implemented as "bot.GetName() - bot.GetVersion()"
        ///     No two bots should be loaded with the same name and version combination
        /// </summary>
        /// <param name="bot">bot to get the Id of</param>
        /// <returns>string identifier of the IBot class</returns>
        public static string GetBotId(IBot bot)
        {
            return bot.Name + "-" + bot.Version;
        }

        /// <summary>
        ///     Sets the actively running bot based on the passed unique keyId
        /// </summary>
        /// <param name="bot">IBot to set as active</param>
        /// <returns>true if bot was set as active; false, if bot is not loaded and was not set as active</returns>
        public static bool SetActiveBot(IBot bot)
        {
            if (!IsBotLoaded(bot))
            {
                LoadBot(bot);
            }
            if (ActiveBot != null && ActiveBot.IsRunning)
            {
                StopActiveBot();
            }

            ActiveBot = LoadedBots[GetBotId(bot)];
            return true;
        }

        /// <summary>
        ///     Attach all manipulation related classes to the passed process.
        ///     ObjectManager and Hook related operations will be available after this call
        /// </summary>
        /// <param name="process"></param>
        public static void AttachToProcess(Process process)
        {
            Memory = new ExternalProcessReader(process);
            if (Offsets.FindOffsets())
            {
                if (DxHook.Instance.Apply())
                {
                    Memory.ProcessExited += (sender, args) => DxHook.Instance.Restore();
                    Logging.Write("Attached to: " + process.Id);
                    return;
                }
                Memory.Dispose();
                Memory = null;
            }
            Logging.Write("Failed to attach to: " + process.Id);
        }

        /// <summary>
        ///     Start the currently Active Bot
        /// </summary>
        public static void StartActiveBot()
        {
            Logging.Write("Starting bot...");
            ActiveBot.StartBot();
        }

        /// <summary>
        ///     Stop the currently Active Bot
        /// </summary>
        public static void StopActiveBot()
        {
            Logging.Write("Stopping bot...");
            ActiveBot.StopBot();
        }

        /// <summary>
        ///     Calls <see cref="IBot.Settings()" /> for the currently ActiveBot
        /// </summary>
        public static void Settings()
        {
            ActiveBot.Settings();
        }

        internal static void StartUp()
        {
            LocalSettings.LoadSettings();

            PluginManager.LoadPlugins();

            PluginManager.StartPlugins();

            Logging.Log("Start Up.");
        }

        internal static void ShutDown()
        {
            StopActiveBot();

            PluginManager.StopPlugins();

            PluginManager.ShutDownPlugins();

            LocalSettings.SaveSettings();

            DxHook.Instance.Restore();

            if (Memory != null)
            {
                Memory.Dispose();
            }


            Logging.Log("Shut Down.");
        }
    }
}