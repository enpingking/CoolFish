using System;
using System.Diagnostics;
using System.Threading;
using CoolFishNS.Management;
using CoolFishNS.Management.CoolManager;
using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.Management.CoolManager.Objects;
using NLog;

namespace CoolFishNS.Bots.FiniteStateMachine.States
{
    /// <summary>
    ///     This state handles if the fishing bobber actively has a fish on the line.
    /// </summary>
    public class StateBobbing : State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Create a timer to timeout after 5 minutes of no caught fish. This is to prevent the bot from running endlessly if
        ///     something unexpected breaks it.
        /// </summary>
        public static Stopwatch BuggedTimer = new Stopwatch();

        private static readonly Random Random = new Random();

        private WoWGameObject _bobber;

        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateBobbing; }
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
                _bobber = ObjectManager.GetSpecificObject(IsBobber, WoWObject.ToWoWGameObject);

                if (_bobber == null)
                {
                    return false;
                }

                return true;
            }
        }

        public override string Name
        {
            get { return "Caught a fish."; }
        }

        private static bool IsBobber(WoWGameObject objectToCheck)
        {
            return objectToCheck.CreatedBy == ObjectManager.PlayerGuid && objectToCheck.AnimationState == 0x440001;
        }

        /// <summary>
        ///     Interact with the bobber so we can catch the fish
        /// </summary>
        public override void Run()
        {
            Logger.Info(Name);
            BuggedTimer.Restart();

            Thread.Sleep(Random.Next(500, 1750));

            BotManager.Memory.Write(Offsets.Addresses["MouseOverGUID"], _bobber.Guid);
            Logger.Info("Clicking bobber");
            DxHook.ExecuteScript("InteractUnit(\"mouseover\");");
            Thread.Sleep(1000);
        }
    }
}