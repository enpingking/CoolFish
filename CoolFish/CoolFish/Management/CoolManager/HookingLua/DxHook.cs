// credits to Ryuk and highvolts from ownedcore.com

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoolFishNS.Management.CoolManager.D3D;
using CoolFishNS.Utilities;
using GreyMagic;

namespace CoolFishNS.Management.CoolManager.HookingLua
{
    /// <summary>
    ///     This class handles Hooking Endscene/Present function so that we can inject ASM if we need to do so.
    /// </summary>
    public class DxHook
    {
        private const int CODECAVESIZE = 0x1000;
        private static readonly DxHook SingletonInstance = new DxHook();
        private readonly byte[] _eraser = new byte[CODECAVESIZE];
        private readonly object _lockObject = new object();
        private readonly Random _random = new Random();

        private readonly string[] _registerNames =
        {
            "ah", "al", "bh", "bl", "ch", "cl", "dh", "dl", "eax",
            "ebx", "ecx", "edx"
        };

        private AllocatedMemory _allocatedMemory;

        private Dirext3D _dxAddress;
        private byte[] _endSceneOriginalBytes;

        private DxHook()
        {
        }

        /// <summary>
        ///     Singleton instance of this class
        /// </summary>
        public static DxHook Instance
        {
            get { return SingletonInstance; }
        }

        /// <summary>
        ///     Determine if the hook is currently applied or not
        /// </summary>
        /// <value>
        ///     <c>true</c> if the hook is applied; otherwise, <c>false</c>.
        /// </value>
        public bool IsApplied { get; private set; }

        private static int Inject(IEnumerable<string> asm, IntPtr address)
        {
            BotManager.Memory.Asm.Clear();
            BotManager.Memory.Asm.SetMemorySize(0x4096);

            foreach (string s in asm)
            {
                BotManager.Memory.Asm.AddLine(s);
            }

            int size = BotManager.Memory.Asm.Assemble().Length;
            bool returnVal = BotManager.Memory.Asm.Inject((uint) address);

            if (!returnVal)
            {
                Logging.Log("Failed to inject code: \n " + BotManager.Memory.Asm.AssemblyString);
            }
            return size;
        }


        private List<string> AddRandomAsm(IEnumerable<string> asm)
        {
            var randomizedList = new List<string>();

            foreach (string a in asm)
            {
                int ranNum = _random.Next(0, 7);
                if (ranNum == 0)
                {
                    randomizedList.Add("nop");
                    if (_random.Next(2) == 1)
                    {
                        randomizedList.Add("nop");
                    }
                }
                else if (ranNum <= 5)
                {
                    randomizedList.Add(GetRandomMov());
                    if (_random.Next(5) == 1)
                    {
                        randomizedList.Add("nop");
                    }
                }
                randomizedList.Add(a);
            }


            return randomizedList;
        }

        private string GetRandomMov()
        {
            int ranNum = _random.Next(0, _registerNames.Length);
            return string.Format("mov {0}, {1}", _registerNames[ranNum], _registerNames[ranNum]);
        }

        /// <summary>
        ///     Apply the DirectX function hook to the WoW process
        /// </summary>
        /// <returns>true if it applied correctly. Otherwise, false</returns>
        internal bool Apply()
        {
            lock (_lockObject)
            {
                //Lets check if we are already hooked.
                if (IsApplied)
                {
                    return true;
                }

                _allocatedMemory = BotManager.Memory.CreateAllocatedMemory(CODECAVESIZE + 0x1000 + 0x4 + 0x4);

                _dxAddress = new Dirext3D(BotManager.Memory.Process);


                // store original bytes
                _endSceneOriginalBytes = BotManager.Memory.ReadBytes(_dxAddress.HookPtr - 5, 10);

                if (_endSceneOriginalBytes[5] == 0xE9)
                {
                    Logging.Log("Detected Another hook");
                    MessageBox.Show(
                        "A hook has already been detected. Please assure you aren't running any other programs that hook DirectX functions in WoW. If this continues, please post for help on the forums.");
                    return false;
                }
                if (_endSceneOriginalBytes[0] == 0xE9)
                {
                    MessageBox.Show(
                        "It seems CoolFish might have crashed before it could clean up after itself. Please restart WoW and reattach the bot.");
                    return false;
                }

                try
                {
                    _allocatedMemory.WriteBytes("codeCavePtr", _eraser);
                    _allocatedMemory.WriteBytes("injectedCode", _eraser);
                    _allocatedMemory.Write("addressInjection", 0);
                    _allocatedMemory.Write("returnInjectionAsm", 0);

                    var asm = new List<string>
                    {
                        "pushad", // save registers to the stack
                        "pushfd",
                        "mov eax, [" + _allocatedMemory["addressInjection"] + "]",
                        "test eax, eax", // Test if you need launch injected code
                        "je @out",
                        "mov eax, [" + _allocatedMemory["addressInjection"] + "]",
                        "call eax", // Launch Function
                        "mov [" + _allocatedMemory["returnInjectionAsm"] + "], eax", // Copy pointer return value
                        "mov edx, " + _allocatedMemory["addressInjection"], // Enter value 0 of so we know we are done
                        "mov ecx, 0",
                        "mov [edx], ecx",
                        "@out:", // Close function
                        "popfd", // load reg
                        "popad"
                    };

                    asm = AddRandomAsm(asm);

                    // injected code

                    int sizeAsm = Inject(asm, _allocatedMemory["injectedCode"]);

                    // Size asm jumpback

                    // copy and save original instructions
                    BotManager.Memory.WriteBytes(IntPtr.Add(_allocatedMemory["injectedCode"], sizeAsm),
                        new[] {_endSceneOriginalBytes[5], _endSceneOriginalBytes[6]});


                    asm.Clear();
                    asm.Add("jmp " + ((uint) _dxAddress.HookPtr + 2)); // short jump takes 2 bytes.

                    // create jump back stub
                    Inject(asm, _allocatedMemory["injectedCode"] + sizeAsm + 2);

                    // create hook jump
                    asm.Clear();
                    asm.Add("@top:");
                    asm.Add("jmp " + _allocatedMemory["injectedCode"]);
                    asm.Add("jmp @top");

                    Inject(asm, _dxAddress.HookPtr - 5);
                    IsApplied = true;
                }
                catch (Exception ex)
                {
                    Logging.Log(ex);
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        ///     Restore the original Endscene function and remove the function hook
        /// </summary>
        internal void Restore()
        {
            lock (_lockObject)
            {
                //Lets check if were hooked
                if (!IsApplied)
                {
                    return;
                }
                if (BotManager.Memory.Process.HasExited)
                {
                    IsApplied = false;
                    return;
                }

                // Restore original endscene:
                BotManager.Memory.WriteBytes(_dxAddress.HookPtr - 5, _endSceneOriginalBytes);

                _allocatedMemory.Dispose();
                _dxAddress.Device.Dispose();

                IsApplied = false;
            }
        }

        /// <summary>
        ///     Inject x86 assembly into the target process and execute it
        /// </summary>
        /// <param name="asm">Assembly code to inject</param>
        /// <returns>true if the code was injected. Otherwise false.</returns>
        private void InjectAndExecute(IEnumerable<string> asm)
        {
            lock (_lockObject)
            {
                if (!IsApplied)
                {
                    throw new Exception(
                        "Tried to inject code when the Hook was not applied");
                }
                //Lets Inject the passed ASM
                Inject(asm, _allocatedMemory["codeCavePtr"]);


                _allocatedMemory.Write("addressInjection", _allocatedMemory["codeCavePtr"]);

                int count = 0;
                while (_allocatedMemory.Read<int>("addressInjection") > 0)
                {
                    Thread.Sleep(1);
                    count++;
                    if (count >= 5000)
                    {
                        throw new Exception("Failed to inject code after 5 seconds");
                    }
                } // Wait to launch code


                _allocatedMemory.WriteBytes("codeCavePtr", _eraser);
            }
        }

        /// <summary>
        ///     Execute custom Lua script into the Wow process
        /// </summary>
        /// <param name="command">Lua code to execute</param>
        /// <exception cref="Exception">Throws generic exception if the function hook we need is not applied</exception>
        public void ExecuteScript(string command)
        {
            if (LocalSettings.Settings["DoDebugging"].As<bool>())
            {
                var stackTrace = new StackTrace();


                Logging.Log("[DEBUG] ExecuteScript Lua from " +
                            stackTrace.GetFrame(1).GetMethod().ReflectedType.Name + "." +
                            stackTrace.GetFrame(1).GetMethod().Name);
            }
            if (command == null)
            {
                throw new Exception("ExecuteScript command can not be null!");
            }

            ExecuteScript(command, new string[0]);
        }

        /// <summary>
        ///     Execute custom LUA code into the Wow process and retrieve a global LUA variable (via GetLocalizedText) all in one
        ///     method
        /// </summary>
        /// <param name="command">Lua code to execute</param>
        /// <param name="returnVariableName">Name of the global LUA variable to return</param>
        /// <exception cref="Exception">Throws generic exception if the function hook we need is not applied</exception>
        public string ExecuteScript(string command, string returnVariableName)
        {
            if (LocalSettings.Settings["DoDebugging"].As<bool>())
            {
                var stackTrace = new StackTrace();


                Logging.Log("[DEBUG] ExecuteScript Lua from " +
                            stackTrace.GetFrame(1).GetMethod().ReflectedType.Name + "." +
                            stackTrace.GetFrame(1).GetMethod().Name);
            }

            if (command == null || returnVariableName == null)
            {
                throw new Exception("ExecuteScript arguments can not be null!");
            }

            return ExecuteScript(command, new[] {returnVariableName})[returnVariableName];
        }

        /// <summary>
        ///     Execute custom LUA code into the Wow process and retrieve a list of global LUA variables (via GetLocalizedText) all
        ///     in one method
        /// </summary>
        /// <param name="executeCommand">Lua code to execute</param>
        /// <param name="commands">Collection of string name of global lua variables to retrieve</param>
        /// <returns>values of the variables to retrieve</returns>
        /// <exception cref="Exception">Throws generic exception if the function hook we need is not applied</exception>
        public Dictionary<string, string> ExecuteScript(string executeCommand, IEnumerable<string> commands)
        {
            if (LocalSettings.Settings["DoDebugging"].As<bool>())
            {
                var stackTrace = new StackTrace();
                Logging.Log("[DEBUG] ExecuteScript (enumerable) Lua from " +
                            stackTrace.GetFrame(1).GetMethod().ReflectedType.Name + "." +
                            stackTrace.GetFrame(1).GetMethod().Name);
            }
            var returnDict = new Dictionary<string, string>();

            if (executeCommand == null || commands == null)
            {
                throw new Exception("ExecuteScript arguments can not be null!");
            }

            List<string> enumerable = commands as List<string> ?? commands.ToList();

            var builder = new StringBuilder(enumerable.Count);

            if (enumerable.Any())
            {
                enumerable.RemoveAll(string.IsNullOrWhiteSpace);
                foreach (string s in enumerable)
                {
                    builder.Append(s);
                    builder.Append('\0');
                    returnDict[s] = string.Empty;
                }
            }
            else
            {
                builder.Append('\0');
            }


            int commandSpace = Encoding.UTF8.GetBytes(builder.ToString()).Length;
            commandSpace += commandSpace%4;
            int commandExecuteSpace = Encoding.UTF8.GetBytes(executeCommand).Length + 1;
            commandExecuteSpace += commandExecuteSpace%4;
            int returnAddressSpace = enumerable.Count == 0 ? 0x4 : enumerable.Count*0x4;

            AllocatedMemory mem =
                BotManager.Memory.CreateAllocatedMemory(commandSpace + commandExecuteSpace + returnAddressSpace +
                                                        0x4 + 0x4);


            mem.WriteBytes("command", Encoding.UTF8.GetBytes(builder.ToString()));
            mem.WriteBytes("commandExecute", Encoding.UTF8.GetBytes(executeCommand));
            if (enumerable.Count != 0)
            {
                mem.WriteBytes("returnVarsPtr", new byte[enumerable.Count*0x4]);
            }
            else
            {
                mem.WriteBytes("returnVarsPtr", new byte[0x4]);
            }

            mem.Write("numberOfReturnVarsAddress", 0);
            mem.Write("returnVarsNamesPtr", mem["command"]);


            try
            {
                InternalExecute(mem["commandExecute"], mem["returnVarsNamesPtr"], enumerable.Count, mem["returnVarsPtr"],
                    mem["numberOfReturnVarsAddress"]);


                if (enumerable.Count > 0)
                {
                    byte[] address = BotManager.Memory.ReadBytes(mem["returnVarsPtr"], enumerable.Count*4);

                    Parallel.ForEach(enumerable, // source collection
                        () => 0, // method to initialize the local variable
                        (value, loop, offset) => // method invoked by the loop on each iteration
                        {
                            var retnByte = new List<byte>();
                            var dwAddress = new IntPtr(BitConverter.ToInt32(address, offset));

                            if (dwAddress != IntPtr.Zero)
                            {
                                var buf = BotManager.Memory.Read<byte>(dwAddress);
                                while (buf != 0)
                                {
                                    retnByte.Add(buf);
                                    dwAddress = dwAddress + 1;
                                    buf = BotManager.Memory.Read<byte>(dwAddress);
                                }
                            }
                            returnDict[value] = Encoding.UTF8.GetString(retnByte.ToArray());
                            offset += 0x4; //modify local variable 
                            return offset; // value to be passed to next iteration
                        },
                        finalResult => { }
                        );
                }
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
            finally
            {
                mem.Dispose();
            }

            return returnDict;
        }

        /// <summary>
        ///     Retrieve a custom global variable in the Lua scope
        /// </summary>
        /// <param name="command">String name of variable to retrieve</param>
        /// <returns>value of the variable to retrieve</returns>
        /// <exception cref="Exception">Throws generic exception if the function hook we need is not applied</exception>
        public string GetLocalizedText(string command)
        {
            if (LocalSettings.Settings["DoDebugging"].As<bool>())
            {
                var stackTrace = new StackTrace();


                Logging.Log("[DEBUG] GetLocalizedText Lua from " +
                            stackTrace.GetFrame(1).GetMethod().ReflectedType.Name + "." +
                            stackTrace.GetFrame(1).GetMethod().Name);
            }

            if (command == null)
            {
                throw new Exception("GetLocalizedText arguments can not be null!");
            }
            string result = GetLocalizedText(new[] {command})[command];

            if (LocalSettings.Settings["DoDebugging"].As<bool>())
            {
                Logging.Write("[DEBUG] result: " + result);
            }
            return result;
        }

        /// <summary>
        ///     Retrieve global LUA variables from the WoW process
        /// </summary>
        /// <param name="commands">String names of variables to retrieve</param>
        /// <returns>values of the variables to retrieve</returns>
        /// <exception cref="Exception">Throws generic exception if the function hook we need is not applied</exception>
        public Dictionary<string, string> GetLocalizedText(IEnumerable<string> commands)
        {
            if (LocalSettings.Settings["DoDebugging"].As<bool>())
            {
                var stackTrace = new StackTrace();


                Logging.Log("[DEBUG] GetLocalizedText (enumerable) Lua from " +
                            stackTrace.GetFrame(1).GetMethod().ReflectedType.Name + "." +
                            stackTrace.GetFrame(1).GetMethod().Name);
            }


            var returnDict = new Dictionary<string, string>();

            if (commands == null)
            {
                return returnDict;
            }

            List<string> enumerable = commands.ToList();

            if (!enumerable.Any())
            {
                return returnDict;
            }
            var builder = new StringBuilder(enumerable.Count);
            enumerable.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (string s in enumerable)
            {
                builder.Append(s);
                builder.Append('\0');
                returnDict[s] = string.Empty;
            }


            int commandSpace = Encoding.UTF8.GetBytes(builder.ToString()).Length;
            commandSpace += commandSpace%4;
            int returnAddressSpace = enumerable.Count == 0 ? 0x4 : enumerable.Count*0x4;

            AllocatedMemory mem =
                BotManager.Memory.CreateAllocatedMemory(commandSpace + returnAddressSpace + 0x4 + 0x4);


            mem.WriteBytes("command", Encoding.UTF8.GetBytes(builder.ToString()));
            mem.WriteBytes("returnVarsPtr", new byte[enumerable.Count*0x4]);
            mem.Write("numberOfReturnVarsAddress", 0);
            mem.Write("returnVarsNamesPtr", mem["command"]);

            try
            {
                InternalExecute(IntPtr.Zero, mem["returnVarsNamesPtr"], enumerable.Count, mem["returnVarsPtr"], mem["numberOfReturnVarsAddress"]);


                byte[] address = BotManager.Memory.ReadBytes(mem["returnVarsPtr"], enumerable.Count*4);

                Parallel.ForEach(enumerable, // source collection
                    () => 0, // method to initialize the local variable
                    (value, loop, offset) => // method invoked by the loop on each iteration
                    {
                        var retnByte = new List<byte>();
                        var dwAddress = new IntPtr(BitConverter.ToInt32(address, offset));

                        if (dwAddress != IntPtr.Zero)
                        {
                            var buf = BotManager.Memory.Read<byte>(dwAddress);
                            while (buf != 0)
                            {
                                retnByte.Add(buf);
                                dwAddress = dwAddress + 1;
                                buf = BotManager.Memory.Read<byte>(dwAddress);
                            }
                        }
                        returnDict[value] = Encoding.UTF8.GetString(retnByte.ToArray());
                        offset += 0x4; //modify local variable 
                        return offset; // value to be passed to next iteration
                    },
                    finalResult => { }
                    );
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
            finally
            {
                mem.Dispose();
            }

            return returnDict;
        }

        private void InternalExecute(IntPtr executeCommandPtr, IntPtr returnVarsNamesPtr, int numberOfReturnVars, IntPtr returnVarsPtr,
            IntPtr numberOfReturnVarsAddress)
        {
            var asm = new List<string>();

            if (executeCommandPtr != IntPtr.Zero)
            {
                // We want to call ExecuteScriptBuffer
                asm.AddRange(new[]
                {
                    "mov eax, " + executeCommandPtr,
                    "push 0",
                    "push eax",
                    "push eax",
                    "mov eax, " + Offsets.Addresses["FrameScript_ExecuteBuffer"],
                    "call eax",
                    "add esp, 0xC"
                });
            }
            if (returnVarsNamesPtr != IntPtr.Zero && numberOfReturnVarsAddress != IntPtr.Zero &&
                returnVarsPtr != IntPtr.Zero)
            {
                // We want to call GetLocalizedText
                asm.AddRange(new[]
                {
                    "call " + Offsets.Addresses["ClntObjMgrGetActivePlayerObj"],
                    "test eax, eax",
                    "je @leave",
                    "mov ebx, eax",
                    "mov esi, " + "[" + numberOfReturnVarsAddress + "]",
                    "mov edi, [" + returnVarsNamesPtr + "]",
                    "mov edx, " + returnVarsPtr,
                    "@start:",
                    "cmp esi, " + numberOfReturnVars,
                    "je @leave",
                    "push edx",
                    "mov ecx, ebx",
                    "push -1",
                    "push edi",
                    "call " + Offsets.Addresses["FrameScript_GetLocalizedText"],
                    "pop edx",
                    "mov [edx], eax", // Copy pointer return value
                    "inc esi",
                    "add edx, 4",
                    "@start_loop:",
                    "inc edi",
                    "mov al, [edi]",
                    "test al, al",
                    "jne @start_loop",
                    "inc edi",
                    "jmp @start",
                    "@leave:"
                });
            }

            asm.Add("retn");
            InjectAndExecute(asm);
        }
    }
}