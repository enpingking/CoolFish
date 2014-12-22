using System;
using System.Diagnostics;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using CoolFishNS.Management;
using CoolFishNS.Management.CoolManager.Objects;
using CoolFishNS.Utilities;
using NLog;

namespace CoolFishNS.Bots.FiniteStateMachine.States
{
    /// <summary>
    ///     This state contains all of the conditions in which we might stop the bot and/or logout
    /// </summary>
    public class StateStopOrLogout : State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Gets a value indicating whether our bags are full or not and we want to stop on this condition.
        /// </summary>
        /// <value>
        ///     <c>true</c> if [bags condition]; otherwise, <c>false</c>.
        /// </value>
        private static bool BagsCondition
        {
            get
            {
                if (UserPreferences.Default.StopOnBagsFull)
                {
                    return PlayerInventory.FreeSlots == 0;
                }
                return false;
            }
        }

        private static bool TimeCondition
        {
            get
            {
                if (UserPreferences.Default.StopTime != null)
                {
                    if (DateTime.Now >= UserPreferences.Default.StopTime)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether we are out of fishing lures and we want to stop on this condition.
        /// </summary>
        /// <value>
        ///     <c>true</c> if [lure condition]; otherwise, <c>false</c>.
        /// </value>
        private bool LureCondition
        {
            get
            {
                if (UserPreferences.Default.StopOnNoLures && !UserPreferences.Default.NoLure)
                {
                    Manager.ExecuteScript("if GetWeaponEnchantInfo() then enchant = 1 else enchant = 0 end;");
                    var result = Manager.GetGlobalVariable("enchant");

                    if (result == "1")
                    {
                        return false;
                    }

                    return !PlayerInventory.HasLures();
                }
                return false;
            }
        }

        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateStopOrLogout; }
        }

        public override bool Run()
        {
            if (TimeCondition)
            {
                Logger.Info("We hit the time limit.");
                StopBot();
                return true;
            }

            if (BagsCondition)
            {
                Logger.Info("Bags are full.");
                StopBot();
                return true;
            }
            if (LureCondition)
            {
                Logger.Info("We ran out of lures.");
                StopBot();
                return true;
            }

            var me = ObjectManager.Me;

            if (me != null && me.Dead)
            {
                Logger.Info("We died :(");
                StopBot();
                return true;
            }

            return false;
        }

        private void StopBot()
        {
            BotManager.StopActiveBot();
            Task.Run(() =>
            {
                for (var i = 0; i < 3; i++)
                {
                    SystemSounds.Hand.Play();
                    Thread.Sleep(3000);
                }
            });

            if (UserPreferences.Default.LogoutOnStop && BotManager.LoggedIn)
            {
                Manager.ExecuteScript("Logout();");
            }

            if (UserPreferences.Default.CloseWoWOnStop)
            {
                Logger.Info("Closing WoW");
                BotManager.DetachFromProcess();
                App.ShutDown();
                Process.GetCurrentProcess().Close();
            }

            if (UserPreferences.Default.ShutdownPcOnStop)
            {
                Process.Start("shutdown", "/s /t 0");
            }
        }
    }
}