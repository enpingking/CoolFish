using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.Properties;
using NLog;

namespace CoolFishNS.Bots.FiniteStateMachine.States
{
    /// <summary>
    ///     This state is run if we caught a fish and have items in the loot window
    /// </summary>
    public class StateDoLoot : State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateDoLoot; }
        }

        /// <summary>
        ///     Gets a value indicating whether [need to run].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [need to run]; otherwise, <c>false</c>.
        /// </value>
        public override bool NeedToRun
        {
            get
            {
                string result = DxHook.Instance.ExecuteScript("loot = IsFishingLoot();", "loot");
                return result == "1";
            }
        }

        public override string Name
        {
            get { return "Looting items"; }
        }

        /// <summary>
        ///     If the loot window is open, then execute the lua to loot the items. See DoLoot.lua in the Resources for code
        /// </summary>
        public override void Run()
        {
            Logger.Info(Name);
            DxHook.Instance.ExecuteScript(Resources.DoLoot);
        }
    }
}