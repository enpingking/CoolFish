using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using GreyMagic.Internals;
using NLog;

namespace CoolFishNS.Management.CoolManager.Internal
{
    internal class HookManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly object LockObject = new object();
        private static readonly Lazy<HookManager> _instance = new Lazy<HookManager>(() => new HookManager());
        private LuaExecuteBuffer _executeBufferHandler;
        private FrameScriptGetText _getTextHandler;
        private bool _isApplied;
        private Dispatcher _mainThreadDispatcher;
        private Detour _worldFrameRenderDetour;

        private HookManager()
        {
        }

        public static HookManager Instance
        {
            get { return _instance.Value; }
        }

        /// <summary>
        ///     Apply the DirectX function hook to the WoW process
        /// </summary>
        internal void Apply()
        {
            lock (LockObject)
            {
                if (_isApplied || BotManager.Memory == null)
                {
                    return;
                }
                _executeBufferHandler =
                    BotManager.Memory.CreateFunction<LuaExecuteBuffer>(Offsets.Addresses["FrameScript_ExecuteBuffer"]);
                _getTextHandler =
                    BotManager.Memory.CreateFunction<FrameScriptGetText>(Offsets.Addresses["FrameScript_GetText"]);
                _worldFrameRenderDetour =
                    BotManager.Memory.Detours.CreateAndApply(
                        BotManager.Memory.CreateFunction<WorldFrameRenderDelegate>(
                            Offsets.Addresses["CGWorldFrame__Render"]), new WorldFrameRenderDelegate(RenderWorld),
                        "CGWorldFrame__Render");
                _isApplied = true;
            }
            while (_mainThreadDispatcher == null)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        ///     Restore the original Endscene function and remove the function hook
        /// </summary>
        internal void Restore()
        {
            lock (LockObject)
            {
                try
                {
                    if (BotManager.Memory != null)
                    {
                        BotManager.Memory.Detours.DeleteAll();
                        BotManager.Memory.Patches.DeleteAll();
                    }
                    
                }
                finally
                {
                    _executeBufferHandler = null;
                    _mainThreadDispatcher = null;
                    _isApplied = false;
                }
            }
        }

        private void RenderWorld(uint ptr)
        {
            _worldFrameRenderDetour.CallOriginal(ptr);
            lock (LockObject)
            {
                if (!_isApplied)
                {
                    return;
                }
                try
                {
                    if (_mainThreadDispatcher == null)
                    {
                        _mainThreadDispatcher = Dispatcher.CurrentDispatcher;
                        _worldFrameRenderDetour.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message, ex);
                    throw;
                }
            }
        }

        /// <summary>
        ///     Execute custom Lua script into the Wow process
        /// </summary>
        /// <param name="command">Lua code to execute</param>
        public void ExecuteScript(string command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }
            if (_mainThreadDispatcher == null || _executeBufferHandler == null)
            {
                return;
            }
            _mainThreadDispatcher.Invoke(() => _executeBufferHandler(command, "", 0));
        }

        /// <summary>
        ///     Retrieve a custom global variable in the Lua scope
        /// </summary>
        /// <param name="command">String name of variable to retrieve</param>
        /// <returns>value of the variable to retrieve</returns>
        public string GetGlobalVariable(string command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }
            if (_mainThreadDispatcher == null || _executeBufferHandler == null)
            {
                return string.Empty;
            }

            var sResult = string.Empty;

            command += '\0';
            var data = Encoding.UTF8.GetBytes(command);
            var memory = Marshal.AllocHGlobal(data.Length);
            try
            {
                Marshal.Copy(data, 0, memory, data.Length);
                var pointer = _mainThreadDispatcher.Invoke(() => _getTextHandler(memory, -1, 2, 0));
                if (pointer != IntPtr.Zero)
                {
                    sResult = BotManager.Memory.ReadString(pointer, Encoding.UTF8);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return sResult;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LuaExecuteBuffer(string lua, string fileName, uint pState);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr FrameScriptGetText(IntPtr command, int negOne, int twoOrThree, int zero);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void WorldFrameRenderDelegate(uint a1);
    }
}