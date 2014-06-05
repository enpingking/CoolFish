using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoolFishNS.Bots.FiniteStateMachine.States;
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
            StateStopOrLogout = 10
        }

        #endregion

        private Task _workerTask;

        /// <summary>
        ///     True if the Engine is running. False otherwise.
        /// </summary>
        public bool Running { get; private set; }

        private List<State> States { get; set; }

        /// <summary>
        ///     Starts the engine.
        /// </summary>
        public void StartEngine()
        {
            if (!BotManager.LoggedIn)
            {
                Logger.Info("Please log into the game first");
            }

            if (_workerTask != null && _workerTask.Status == TaskStatus.Running)
            {
                Logger.Info("The bot is already running.");
            }

            Running = true;
            _workerTask = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        ///     Stops the engine.
        /// </summary>
        public void StopEngine()
        {
            if (_workerTask == null || _workerTask.Status != TaskStatus.Running)
            {
                // Nothing to do.
                Logger.Info("The bot is not running");
                return;
            }

            Running = false;
            if (!_workerTask.Wait(5000))
            {
                Logger.Warn("Bot thread failed to stop on its own. Status: " + _workerTask.Status);
            }
        }

        private void AddStates()
        {
            States = new List<State> {new StateDoNothing(), new StateStopOrLogout()};

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


            States.Sort();
        }

        private void InitOptions()
        {
            AddStates();
            StateBobbing.BuggedTimer.Restart();
            var builder = new StringBuilder();

            foreach (SerializableItem serializableItem in LocalSettings.Items)
            {
                builder.Append("\"" + serializableItem.Value + "\",");
            }
            string items = builder.ToString();

            if (items.Length > 0)
            {
                items = items.Remove(items.Length - 1);
            }

            builder.Clear();
            builder.Append("ItemsList = {" + items + "} \n");
            builder.Append("LootLeftOnly = " +
                           LocalSettings.Settings["LootOnlyItems"].ToString()
                               .ToLower() + " \n");
            builder.Append("DontLootLeft = " +
                           LocalSettings.Settings["DontLootLeft"].ToString().ToLower() + " \n");
            builder.Append("LootQuality = " + LocalSettings.Settings["LootQuality"] + " \n");
            builder.Append(Resources.WhisperNotes + " \n");
            builder.Append("LootLog = {} \n");
            builder.Append("NoLootLog = {} \n");
            builder.Append("DODEBUG = " +
                           ((LocalSettings.Settings["LogLevel"] == LogLevel.Debug.Ordinal ||
                             LocalSettings.Settings["LogLevel"] == LogLevel.Trace.Ordinal)
                               ? "true"
                               : "false"));

            BotManager.DxHookInstance.ExecuteScript(builder.ToString());
        }


        private void Run()
        {
            try
            {
                InitOptions();
                Logger.Info("Started Engine");
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
            }
            catch (Exception ex)
            {
                Logger.Error("Unhandled error occurred", ex);
            }

            try
            {
                if (BotManager.LoggedIn)
                {
                    BotManager.DxHookInstance.ExecuteScript(
                        "if CoolFrame then CoolFrame:UnregisterAllEvents(); end print(\"|cff00ff00---Loot Log---\"); for key,value in pairs(LootLog) do local _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end print(\"|cffff0000---DID NOT Loot Log---\"); for key,value in pairs(NoLootLog) do _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurred while unregistering events", ex);
            }

            Logger.Info("Engine Stopped");
        }
    }
}