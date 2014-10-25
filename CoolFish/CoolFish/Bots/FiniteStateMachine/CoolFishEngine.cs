using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoolFishNS.Bots.FiniteStateMachine.States;
using CoolFishNS.Exceptions;
using CoolFishNS.Management;
using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.Properties;
using CoolFishNS.Utilities;
using MarkedUp;
using NLog;
using LogLevel = NLog.LogLevel;

namespace CoolFishNS.Bots.FiniteStateMachine
{
    /// <summary>
    ///     The main driving Engine of the Finite State Machine. This performs all the state running logic.
    /// </summary>
    public class CoolFishEngine
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly object LockObject = new object();

        #region StatePriority enum

        /// <summary>
        ///     Simple priority list for all the states in our FSM
        /// </summary>
        public enum StatePriority
        {
            StateFish = 0,
            StateUseRumsey,
            StateUseSpear,
            StateApplyLure,
            StateUseCharm,
            StateUseRaft,
            StateRunScripts,
            StateDoNothing,
            StateBobbing,
            StateDoLoot,
            StateDoWhisper,
            StateStopOrLogout = 11
        }

        #endregion

        /// <summary>
        ///     True if the Engine is running. False otherwise.
        /// </summary>
        public volatile bool Running;

        private Task _workerTask;

        private SortedSet<State> States { get; set; }

        /// <summary>
        ///     Starts the engine.
        /// </summary>
        public void StartEngine()
        {
            lock (LockObject)
            {
                if (!BotManager.LoggedIn)
                {
                    Logger.Info("Please log into the game first");
                }

                if (Running)
                {
                    Logger.Info("The engine is running");
                    return;
                }

                Running = true;
                _workerTask = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        ///     Stops the engine.
        /// </summary>
        public void StopEngine()
        {
            lock (LockObject)
            {
                if (!Running)
                {
                    Logger.Info("The engine is stopped");
                    return;
                }

                Running = false;
                if (_workerTask != null)
                {
                    _workerTask.Wait(5000);
                    _workerTask = null;
                }
            }
        }

        private void AddStates()
        {
            States = new SortedSet<State> {new StateDoNothing(), new StateStopOrLogout()};

            if (UserPreferences.Default.DoFishing)
            {
                States.Add(new StateFish());
            }
            if (UserPreferences.Default.DoBobbing)
            {
                States.Add(new StateBobbing());
            }
            if (UserPreferences.Default.DoLoot)
            {
                States.Add(new StateDoLoot());
            }
            if (!UserPreferences.Default.NoLure)
            {
                States.Add(new StateApplyLure());
            }
            if (UserPreferences.Default.UseCharm)
            {
                States.Add(new StateUseCharm());
            }
            if (UserPreferences.Default.UseRaft)
            {
                States.Add(new StateUseRaft());
            }
            if (UserPreferences.Default.SoundOnWhisper)
            {
                States.Add(new StateDoWhisper());
            }
            if (UserPreferences.Default.UseRumsey)
            {
                States.Add(new StateUseRumsey());
            }
            if (UserPreferences.Default.UseSpear)
            {
                States.Add(new StateUseSpear());
            }
        }

        private void InitOptions()
        {
            AddStates();
            StateBobbing.BuggedTimer.Restart();
            var builder = new StringBuilder();
            foreach (SerializableItem serializableItem in UserPreferences.Default.Items.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
            {
                builder.Append("[\"");
                builder.Append(serializableItem.Value);
                builder.Append("\"] = true, ");
            }
            string items = builder.ToString();

            if (items.Length > 0)
            {
                items = items.Remove(items.Length - 2);
            }

            builder.Clear();
            builder.AppendLine("ItemsList = {" + items + "}");
            builder.AppendLine("LootLeftOnly = " +
                               UserPreferences.Default.LootOnlyItems.ToString().ToLower());
            builder.AppendLine("DontLootLeft = " +
                               UserPreferences.Default.DontLootLeft.ToString().ToLower());
            builder.AppendLine("LootQuality = " + UserPreferences.Default.LootQuality);
            builder.AppendLine(Resources.WhisperNotes);
            builder.AppendLine("LootLog = {}; ");
            builder.AppendLine("NoLootLog = {}; ");
            builder.Append("DODEBUG = " +
                           ((UserPreferences.Default.LogLevel == LogLevel.Debug.Ordinal ||
                             UserPreferences.Default.LogLevel == LogLevel.Trace.Ordinal)
                               ? "true"
                               : "false"));

            DxHook.ExecuteScript(builder.ToString());
        }

        private void Pulse()
        {
            foreach (State state in States)
            {
                if (!Running || !BotManager.LoggedIn)
                {
                    return;
                }
                if (state.Run())
                {
                    return;
                }
            }
        }

        private void Run()
        {
            Logger.Info("Started Engine");

            try
            {
                InitOptions();
                while (Running && BotManager.LoggedIn)
                {
                    Pulse();
                    Thread.Sleep(1000/60);
                }

                if (BotManager.LoggedIn)
                {
                    DxHook.ExecuteScript(
                        "if CoolFrame then CoolFrame:UnregisterAllEvents(); end print(\"|cff00ff00---Loot Log---\"); for key,value in pairs(LootLog) do local _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end print(\"|cffff0000---DID NOT Loot Log---\"); for key,value in pairs(NoLootLog) do _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end");
                }
            }
            catch (CodeInjectionFailedException ex)
            {
                const string msg = "Stopping bot because we could not execute code required to continue";
                if (DxHook.TriedHackyHook)
                {
                    AnalyticClient.SessionEvent("TriedHackyHook");
                    Logger.Warn(msg, (Exception) ex);
                    Logger.Info(
                        "It seems you tried to create the hook CoolFish needs despite the mention of problems it could cause. This error is likely a result of that. It is recommended that you stop running the interfering program.");
                }
                else
                {
                    Logger.Error(msg, (Exception) ex);
                }
            }
            catch (HookNotAppliedException ex)
            {
                Logger.Warn("Stopping bot because required hook is no longer applied", (Exception) ex);
            }
            catch (AccessViolationException ex)
            {
                Logger.Warn("Stopping bot because we failed to read memory.", (Exception) ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Unhandled error occurred. LoggedIn: " + BotManager.LoggedIn + " Attached: " + BotManager.IsAttached, ex);
            }

            Running = false;
            Logger.Info("Engine Stopped");
        }
    }
}