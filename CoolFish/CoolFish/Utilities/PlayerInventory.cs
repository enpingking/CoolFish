using System;
using System.Collections.Generic;
using CoolFishNS.Management.CoolManager.HookingLua;
using CoolFishNS.Properties;
using NLog;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     All functions relating to the Player's in game inventory is stored here
    /// </summary>
    public static class PlayerInventory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Gets the number of free inventory slots. (Note: only cares about absolute number. Doesn't matter what bag type it
        ///     is)
        /// </summary>
        /// <value>
        ///     The number of free slots.
        /// </value>
        public static int FreeSlots
        {
            get
            {
                string slots =
                    DxHook.ExecuteScript(
                        "CFFreeslots = 0; for i=0,4 do local count = GetContainerNumFreeSlots(i); CFFreeslots = CFFreeslots + count; end ",
                        "CFFreeslots");

                if (string.IsNullOrEmpty(slots))
                {
                    Logger.Warn("Unable to determine free bag space.");
                    return 0;
                }

                return Convert.ToInt32(slots);
            }
        }


        /// <summary>
        ///     Gets the number of fishing lures in the players inventory. Returns 1 if using the fishing hats in the game.
        /// </summary>
        /// <value>
        ///     The lure count.
        /// </value>
        public static uint LureCount
        {
            get
            {
                Dictionary<string, string> count = DxHook.ExecuteScript(Resources.GetLureName, new[] {"Count", "LureName"});

                if (count["Count"] == "1" && !string.IsNullOrEmpty(count["LureName"]))
                {
                    return 1;
                }
                return 0;
            }
        }
    }
}