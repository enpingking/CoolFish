using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using NLog;

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

        private Task _workerTask;

        /// <summary>
        ///     True if the Engine is running. False otherwise.
        /// </summary>
        public bool Running { get; private set; }

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
            States = new SortedSet<State>{new StateDoNothing(), new StateStopOrLogout()};
            
            if (LocalSettings.Settings["DoFishing"])
            {
                States.Add(new StateFish());
            }
            if (LocalSettings.Settings["DoBobbing"])
            {
                States.Add(new StateBobbing());
            }
            if (LocalSettings.Settings["DoLoot"])
            {
                States.Add(new StateDoLoot());
            }
            if (!LocalSettings.Settings["NoLure"])
            {
                States.Add(new StateApplyLure());
            }
            if (LocalSettings.Settings["UseCharm"])
            {
                States.Add(new StateUseCharm());
            }
            if (LocalSettings.Settings["UseRaft"])
            {
                States.Add(new StateUseRaft());
            }
            if (LocalSettings.Settings["SoundOnWhisper"])
            {
                States.Add(new StateDoWhisper());
            }
            if (LocalSettings.Settings["UseRumsey"])
            {
                States.Add(new StateUseRumsey());
            }
            if (LocalSettings.Settings["UseSpear"])
            {
                States.Add(new StateUseSpear());
            }


            
        }

        private void InitOptions()
        {
            AddStates();
            StateBobbing.BuggedTimer.Restart();
            var builder = new StringBuilder();
            foreach (SerializableItem serializableItem in LocalSettings.Items.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
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
                           LocalSettings.Settings["LootOnlyItems"].ToString().ToLower());
            builder.AppendLine("DontLootLeft = " +
                           LocalSettings.Settings["DontLootLeft"].ToString().ToLower());
            builder.AppendLine("LootQuality = " + LocalSettings.Settings["LootQuality"]);
            builder.AppendLine(Resources.WhisperNotes);
            builder.AppendLine("LootLog = {}");
            builder.AppendLine("NoLootLog = {}");
            builder.Append("DODEBUG = " +
                           ((LocalSettings.Settings["LogLevel"] == LogLevel.Debug.Ordinal ||
                             LocalSettings.Settings["LogLevel"] == LogLevel.Trace.Ordinal)
                               ? "true"
                               : "false"));

            DxHook.ExecuteScript(builder.ToString());
        }

        private void Run()
        {
            Logger.Info("Started Engine");

            try
            {
                InitOptions();
                while (Running && BotManager.LoggedIn)
                {
                    // This starts at the highest priority state,
                    // and iterates its way to the lowest priority.
                    foreach (State state in States.TakeWhile(state => Running && BotManager.LoggedIn).Where(state => state.NeedToRun))
                    {
                        state.Run();

                        // Break out of the iteration,
                        // as we found a state that has run.
                        // We don't want to run any more states
                        // this time around.
                        break;
                    }

                    Thread.Sleep(1000/60);
                }

                if (BotManager.LoggedIn)
                {
                    DxHook.ExecuteScript(
                        "if CoolFrame then CoolFrame:UnregisterAllEvents(); end print(\"|cff00ff00---Loot Log---\"); for key,value in pairs(LootLog) do local _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end print(\"|cffff0000---DID NOT Loot Log---\"); for key,value in pairs(NoLootLog) do _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end");
                }
            }
            catch (CodeInjectionFailedException)
            {
                Logger.Warn("Stopping bot because we could not execute code required to continue");
            }
            catch (HookNotAppliedException)
            {
                Logger.Warn("Stopping bot because required hook is no longer applied");
            }
            catch (AccessViolationException)
            {
                Logger.Warn("Stopping bot because we failed to read memory.");
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