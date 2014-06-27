using System;
using System.Diagnostics;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using CoolFishNS.Management;
using CoolFishNS.Management.CoolManager.HookingLua;
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
                if (LocalSettings.Settings["StopOnBagsFull"])
                {
                    return PlayerInventory.FreeSlots == 0;
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
        private static bool LureCondition
        {
            get
            {
                if (LocalSettings.Settings["StopOnNoLures"] &&
                    !LocalSettings.Settings["NoLure"])
                {
                    string result = DxHook.ExecuteScript("enchant = GetWeaponEnchantInfo();", "enchant");

                    if (result == "1")
                    {
                        return false;
                    }

                    return PlayerInventory.LureCount == 0;
                }
                return false;
            }
        }


        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateStopOrLogout; }
        }


        public override bool NeedToRun
        {
            get
            {
                bool dead = false;
                if (ObjectManager.Me != null)
                {
                    dead = ObjectManager.Me.Dead;
                }

                return BagsCondition || LureCondition || dead ||
                       StateBobbing.BuggedTimer.ElapsedMilliseconds > 1000*60*3;
            }
        }

        public override void Run()
        {
            if (BagsCondition)
            {
                Logger.Info("Bags are full.");
            }
            if (LureCondition)
            {
                Logger.Info("We ran out of lures.");
            }

            if (ObjectManager.Me != null)
            {
                if (ObjectManager.Me.Dead)
                {
                    Logger.Info("We died :(");
                }
            }

            if (StateBobbing.BuggedTimer.ElapsedMilliseconds > 1000*60*3)
            {
                Logger.Info("We haven't gotten a bobber in 3 minutes. Somethings wrong.");
                StateBobbing.BuggedTimer.Stop();
            }

            BotManager.StopActiveBot();

            Task.Run(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    SystemSounds.Hand.Play();
                    Thread.Sleep(3000);
                }
            });

            if (LocalSettings.Settings["LogoutOnStop"])
            {
                DxHook.ExecuteScript("Logout();");
            }

            if (LocalSettings.Settings["ShutdownPConStop"])
            {
                Process.Start("shutdown", "/s /t 0");
            }

            if (LocalSettings.Settings["CloseWoWonStop"])
            {
                var proc = BotManager.Memory.Process;
                BotManager.DetachFromProcess();
                proc.CloseMainWindow();
                proc.Close();
                App.ShutDown();
                Environment.Exit(0);

            }


        }
    }
}