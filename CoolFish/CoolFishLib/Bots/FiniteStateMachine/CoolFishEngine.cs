using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoolFishNS.Bots.FiniteStateMachine.States;
using CoolFishNS.Management;
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
            StateApplyBait,
            StateUseRaft,
            StateDoNothing,
            StateBobbing,
            StateDoLoot,
            StateDoWhisper,
            StateStopOrLogout
        }

        #endregion

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly object LockObject = new object();
        private Task _workerTask;
        private readonly IScriptManager _manager;
        private readonly SortedSet<State> _states;

        public CoolFishEngine(IScriptManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            _manager = manager;
            _states = new SortedSet<State>
            {
                new StateDoNothing(),
                new StateStopOrLogout(),
                new StateFish(),
                new StateBobbing(),
                new StateDoLoot(),
                new StateApplyLure(),
                new StateUseRaft(),
                new StateDoWhisper(),
                new StateUseRumsey(),
                new StateUseSpear(),
                new StateApplyBait()
            };
        }

        /// <summary>
        ///     True if the Engine is running. False otherwise.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        ///     Starts the engine.
        /// </summary>
        public void StartEngine()
        {
            lock (LockObject)
            {
                if (Running)
                {
                    Logger.Info("The engine is running");
                    return;
                }
                Running = true;
                Task.Run(() => Run());
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
            }
        }

        private void InitOptions()
        {
            UserPreferences.Default.StopTime = UserPreferences.Default.StopOnTime
                ? (DateTime?) DateTime.Now.AddMinutes(UserPreferences.Default.MinutesToStop)
                : null;


            var builder = new StringBuilder();
            foreach (
                var serializableItem in
                    UserPreferences.Default.Items.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
            {
                builder.Append("[\"");
                builder.Append(serializableItem.Value);
                builder.Append("\"] = true, ");
            }
            var items = builder.ToString();

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

            if (UserPreferences.Default.BaitItem != null)
            {
                builder.AppendLine("BaitItemId = " + UserPreferences.Default.BaitItem.Value);
                builder.AppendLine("BaitSpellId = " + UserPreferences.Default.BaitItem.ValueTwo);
            }
            builder.AppendLine("LootLog = {}; ");
            builder.AppendLine("NoLootLog = {}; ");

            _manager.ExecuteScript(builder.ToString());
        }

        private void Pulse()
        {
            foreach (var state in _states)
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
                    _manager.ExecuteScript(
                        "if CoolFrame then CoolFrame:UnregisterAllEvents(); end print(\"|cff00ff00---Loot Log---\"); for key,value in pairs(LootLog) do local _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end print(\"|cffff0000---DID NOT Loot Log---\"); for key,value in pairs(NoLootLog) do _, itemLink = GetItemInfo(key); print(itemLink .. \": \" .. value) end");
                }
            }
            catch (AccessViolationException ex)
            {
                Logger.Warn("Stopping bot because we failed to read memory.", (Exception) ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Unhandled error occurred", ex);
            }
            Running = false;
            Logger.Info("Engine Stopped");
        }
    }
}