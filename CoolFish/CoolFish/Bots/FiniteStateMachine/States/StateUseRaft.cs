using System.Threading;
using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.Properties;
using NLog;

namespace CoolFishNS.Bots.FiniteStateMachine.States
{
    /// <summary>
    ///     Run this state if we want to use water walking or Angler's raft item to fish in open water
    /// </summary>
    public class StateUseRaft : State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateUseRaft; }
        }

        /// <summary>
        ///     Gets a value indicating whether [need to run].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [need to run]; otherwise, <c>false</c>.
        /// </value>
        private bool NeedToRun
        {
            get
            {
                string res = DxHook.ExecuteScript(Resources.NeedToRunUseRaft, "expires");

                return res == "1";
            }
        }

        public override string Name
        {
            get { return "Using water walk"; }
        }

        /// <summary>
        ///     Execute Lua code to use Raft/Water Walking. See UseRaft.lua in Resources for code.
        /// </summary>
        public override bool Run()
        {
            if (!NeedToRun)
            {
                return false;
            }
            Logger.Info(Name);
            DxHook.ExecuteScript(Resources.UseRaft);
            Thread.Sleep(1000);
            return true;
        }
    }
}