using System;
using CoolFishNS.Management.CoolManager.Internal;

namespace CoolFishNS.Management.CoolManager.Lua
{
    /// <summary>
    ///     This class handles hooking necessary functions for the bot
    /// </summary>
    internal class WoWScriptManager : IScriptManager
    {
        private readonly HookManager _manager;

        internal WoWScriptManager(HookManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            _manager = manager;
        }

        /// <summary>
        ///     Execute custom Lua script into the Wow process
        /// </summary>
        /// <param name="command">Lua code to execute</param>
        public void ExecuteScript(string command)
        {
            _manager.ExecuteScript(command);
        }

        /// <summary>
        ///     Retrieve a custom global variable in the Lua scope
        /// </summary>
        /// <param name="command">String name of variable to retrieve</param>
        /// <returns>value of the variable to retrieve</returns>
        public string GetGlobalVariable(string command)
        {
            return _manager.GetGlobalVariable(command);
        }
    }
}