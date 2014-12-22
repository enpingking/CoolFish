﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using CoolFishNS.Bots;
using CoolFishNS.Bots.CoolFishBot;
using CoolFishNS.Management.CoolManager;
using CoolFishNS.Management.CoolManager.Internal;
using CoolFishNS.PluginSystem;
using GreyMagic;
using NLog;

namespace CoolFishNS.Management
{
    /// <summary>
    ///     Bot manager class for performing operations on IBot implemented classes
    /// </summary>
    public static class BotManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly object LockObject = new object();
        internal static readonly Dictionary<string, IBot> LoadedBots = new Dictionary<string, IBot>();

        static BotManager()
        {
            var bot = new CoolFishBot(new ScriptManagerFactory().GetInstance());
            LoadBot(bot);
            SetActiveBot(bot);
        }

        /// <summary>
        ///     The main ExternalProcessReader object that reads/writes memory to the attached process
        /// </summary>
        internal static InProcessMemoryReader Memory { get; private set; }

        /// <summary>
        ///     Returns true if we are attached to a Wow process and can perform memory operations and Manager methods
        /// </summary>
        public static bool IsAttached { get; private set; }

        /// <summary>
        ///     Currently active IBot object that the user interface will interact with.
        ///     This field should be set to whatever Bot object you want to respond to UI functions (Start, Stop, etc.)
        /// </summary>
        public static IBot ActiveBot { get; private set; }

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
                    return IsAttached && Memory.Read<bool>(Offsets.Addresses["LoadingScreen"]);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Error checking whether we are logged in", ex);
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
            var id = GetBotId(botToLoad);
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
        public static void AttachToProcess()
        {
            lock (LockObject)
            {
                try
                {
                    if (IsAttached)
                    {
                        return;
                    }
                    var process = Process.GetCurrentProcess();
                    if (Offsets.FindOffsets())
                    {
                        Memory = new InProcessMemoryReader(process);
                        HookManager.Instance.Apply();
                        IsAttached = true;
                        Logger.Info("Attached to: " + process.Id);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to attach do to an exception.", ex);
                    Memory = null;
                }
            }
        }

        public static void DetachFromProcess()
        {
            lock (LockObject)
            {
                try
                {
                    HookManager.Instance.Restore();
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception thrown while detaching from process", ex);
                }
                Memory = null;
                IsAttached = false;
                Logger.Info("Detached from process");
            }
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
                Logger.Error("Exception thrown while trying to start the bot", ex);
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
                Logger.Error("Exception thrown while trying to stop the bot", ex);
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
                Logger.Error("Exception thrown while trying to modify bot settings", ex);
            }
        }

        internal static void StartUp()
        {
            try
            {
                PluginManager.LoadPlugins();

                PluginManager.StartPlugins();
            }
            catch (Exception ex)
            {
                Logger.Error("Exception on StartUp", ex);
            }

            Logger.Debug("Start Up.");
        }

        internal static void ShutDown()
        {
            try
            {
                StopActiveBot();

                DetachFromProcess();

                PluginManager.StopPlugins();

                PluginManager.ShutDownPlugins();
            }
            catch (Exception ex)
            {
                Logger.Error("Exception thrown on ShutDown", ex);
            }

            Logger.Debug("Shut Down.");
        }
    }
}