using System;
using CoolFishNS.Management.CoolManager.Internal;
using CoolFishNS.Management.CoolManager.Lua;

namespace CoolFishNS.Management
{
    /// <summary>
    ///     Factoring for getting new instances of the IScriptManager interface
    /// </summary>
    public class ScriptManagerFactory
    {
        private static readonly Lazy<IScriptManager> Instance =
            new Lazy<IScriptManager>(() => new WoWScriptManager(HookManager.Instance));

        /// <summary>
        ///     Gets a new instance of an IScriptManager (This instance could be shared, but should never be null)
        /// </summary>
        /// <returns>An instance of IScriptManager</returns>
        public IScriptManager GetInstance()
        {
            return Instance.Value;
        }
    }
}