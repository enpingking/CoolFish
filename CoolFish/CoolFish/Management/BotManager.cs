using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CoolFishNS.Bots;
using CoolFishNS.Bots.CoolFishBot;
using CoolFishNS.Management.CoolManager;
using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.PluginSystem;
using CoolFishNS.Utilities;
using GreyMagic;
using MarkedUp;
using NLog;

namespace CoolFishNS.Management
{
    /// <summary>
    ///     Bot manager class for performing operations on IBot implemented classes
    /// </summary>
    public static class BotManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        /// The main DxHook object that performs operations on the currently attached process
        /// </summary>
        public static DxHook DxHookInstance { get; private set; }

        /// <summary>
        ///     Returns true if we are attached to a Wow process and can perform memory operations and DXHook methods
        /// </summary>
        public static bool IsAttached
        {
            get { return Memory != null && Memory.IsProcessOpen && DxHookInstance != null; }
        }

        /// <summary>
        ///     Currently active IBot object that the user interface will interact with.
        ///     This field should be set to whatever Bot object you want to respond to UI functions (Start, Stop, etc.)
        /// </summary>
        internal static IBot ActiveBot { get; private set; }

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
                    Logger.ErrorException("Error reading ToonName", ex);
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
                    Logger.ErrorException("Error checking whether we are logged in", ex);
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
                Logger.Info("Loaded " + id);
                LoadedBots[id] = botToLoad;
            }
            else
            {
                Logger.Info("Bot " + id + " has already been loaded. Skipping load...");
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
            try
            {
                if (process.HasExited)
                {
                    Logger.Warn("The process you have selected has exited. Please select another.");
                    return;
                }
                StopActiveBot();
                if (Offsets.FindOffsets(process))
                {
                    Memory = new ExternalProcessReader(process);
                    DxHookInstance = new DxHook(process);
                    if (DxHookInstance.Apply())
                    {
                        Memory.ProcessExited += (sender, args) => BotManager.DxHookInstance.Restore();
                        Logger.Info("Attached to: " + process.Id);
                        return;
                    }
                    Memory.Dispose();
                    Memory = null;
                    DxHookInstance = null;
                }
            }
            catch (FileNotFoundException ex)
            {
                if (Memory != null)
                {
                    Memory.Dispose();
                    Memory = null;
                }
                if (ex.FileName.Contains("fasmdll_managed"))
                {
                    AnalyticClient.SessionEvent("Missing Redistributable");
                    Logger.Fatal(
                        "You have not downloaded a required prerequisite for CoolFish. Please visit the following download page for the Visual C++ Redistributable: http://www.microsoft.com/en-us/download/details.aspx?id=40784 (Download the vcredist_x86.exe when asked)");
                }
                else
                {
                    throw;
                }
            }
            catch(Exception ex)
            {
                Logger.ErrorException("Failed to attach do to an exception.", ex);
            }

            Logger.Warn("Failed to attach to: " + process.Id);
        }

        public static void DetatchFromProcess()
        {
            
        }

        /// <summary>
        ///     Start the currently Active Bot
        /// </summary>
        public static void StartActiveBot()
        {
            try
            {
                if (ActiveBot != null && !ActiveBot.IsRunning)
                {
                    Logger.Info("Starting bot...");
                    ActiveBot.StartBot();
                }
            }
            catch (Exception ex)
            {
                
                Logger.ErrorException("Exception thrown while trying to start the bot", ex);
            }
           
        }

        /// <summary>
        ///     Stop the currently Active Bot
        /// </summary>
        public static void StopActiveBot()
        {
            try
            {
                if (ActiveBot != null && ActiveBot.IsRunning)
                {
                    Logger.Info("Stopping bot...");
                    ActiveBot.StopBot();
                }
            }
            catch (Exception ex)
            {
                
               Logger.ErrorException("Exception thrown while trying to stop the bot", ex);
            }
            
        }

        /// <summary>
        ///     Calls <see cref="IBot.Settings()" /> for the currently ActiveBot
        /// </summary>
        public static void Settings()
        {
            try
            {
                ActiveBot.Settings();
            }
            catch (Exception ex)
            {

                Logger.ErrorException("Exception thrown while trying to modify bot settings", ex);
            }
           
        }

        internal static void StartUp()
        {
            LocalSettings.LoadSettings();

            PluginManager.LoadPlugins();

            PluginManager.StartPlugins();

            Logger.Debug("Start Up.");
        }

        internal static void ShutDown()
        {
            StopActiveBot();

            PluginManager.StopPlugins();

            PluginManager.ShutDownPlugins();

            LocalSettings.SaveSettings();

            if (DxHookInstance != null)
            {
                DxHookInstance.Restore();
            }
            
            if (Memory != null)
            {
                Memory.Dispose();
            }


            Logger.Debug("Shut Down.");
        }
    }
}