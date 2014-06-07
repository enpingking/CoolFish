using System.Threading;
using CoolFishNS.Management;
using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.Utilities;
using NLog;

namespace CoolFishNS.Bots.FiniteStateMachine.States
{
    /// <summary>
    ///     State which handles applying a fishing lure if we need one
    /// </summary>
    public class StateApplyLure : State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateApplyLure; }
        }

        /// <summary>
        ///     Gets a value indicating whether we [need to run] this state or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> if we [need to run]; otherwise, <c>false</c>.
        /// </value>
        public override bool NeedToRun
        {
            get
            {
                string result = BotManager.DxHookInstance.ExecuteScript("enchant = GetWeaponEnchantInfo();", "enchant");

                if (result == "1")
                {
                    return false;
                }

                return PlayerInventory.LureCount > 0;
            }
        }

        public override string Name
        {
            get { return "Applying lure"; }
        }

        /// <summary>
        ///     Runs this state and apply the lure.
        /// </summary>
        public override void Run()
        {
            Logger.Info(Name);

            BotManager.DxHookInstance.ExecuteScript("RunMacroText(\"/use \" .. LureName);");

            Thread.Sleep(3000);
        }
    }
}