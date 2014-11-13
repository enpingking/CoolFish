namespace CoolFishNS.Management
{
    /// <summary>
    ///     Interface defining standard scripting tasks
    /// </summary>
    public interface IScriptManager
    {
        /// <summary>
        ///     Executes arbitrary scripting payload into the currently selected WoW process
        /// </summary>
        /// <param name="luaCode">lua code to execute</param>
        void ExecuteScript(string luaCode);

        /// <summary>
        ///     Gets a globally scoped lua variable from the currently selected WoW process
        /// </summary>
        /// <param name="variableName">Name of the variable as it is defined in game</param>
        /// <returns>The value of the variable. Empty string if it is not defined</returns>
        string GetGlobalVariable(string variableName);
    }
}