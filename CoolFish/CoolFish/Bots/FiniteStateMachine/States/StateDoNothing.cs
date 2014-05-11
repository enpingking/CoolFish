using System;
using CoolFishNS.Management.CoolManager.Objects;
using CoolFishNS.Utilities;
using NLog;

namespace CoolFishNS.Bots.FiniteStateMachine.States
{
    /// <summary>
    ///     This state is run if we are moving or get into combat. This prevents the bot from trying to do anything when it is
    ///     unable to do so.
    /// </summary>
    public class StateDoNothing : State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateDoNothing; }
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
                return ObjectManager.Me.Combat || ObjectManager.Me.Speed > 0;
            }
        }

        /// <summary>
        ///     Do "nothing"
        /// </summary>
        public override void Run()
        {
            Logger.Debug("[DoNothingState]");
        }
    }
}