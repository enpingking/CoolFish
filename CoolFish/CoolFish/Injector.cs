using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NLog;

namespace CoolFish
{
    public class Injector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string DefaultPayload = "DomainWrapper.dll", DefaultExport = "Host";

        public void Inject(Process proc)
        {
            var payload = Path.GetFullPath(DefaultPayload);

            var handle = Imports.OpenProcess(
                ProcessAccessFlags.QueryInformation | ProcessAccessFlags.CreateThread |
                ProcessAccessFlags.VMOperation | ProcessAccessFlags.VMWrite |
                ProcessAccessFlags.VMRead, false, proc.Id);
            if (handle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            IntPtr pLibBase = IntPtr.Zero;
            try
            {
                pLibBase = Inject(handle, proc, payload);
            }
            catch (Win32Exception ex)
            {
                Logger.Warn("Please run WoW as administrator mode.", (Exception)ex);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
            }
            finally
            {
                if (pLibBase != IntPtr.Zero)
                {
                    Eject(handle, pLibBase);
                }
            }
        }



        private static IntPtr Inject(IntPtr hTarget, Process targetProc, string payload, bool final = false)
        {
            if (!File.Exists(payload))
                throw new FileNotFoundException("Can't find " + payload + " to inject");

            var payloadName = Path.GetFileName(payload);
            var libPathSize = (uint)Encoding.Unicode.GetByteCount(payload);

            IntPtr
                pLibPath = Marshal.StringToHGlobalUni(payload),
                pExternLibPath = IntPtr.Zero;
            try
            {
                var hKernel = Imports.GetModuleHandle("kernel32");
                var pLoadLib = Imports.GetProcAddress(hKernel, "LoadLibraryW");

                pExternLibPath = Imports.VirtualAllocEx(hTarget, IntPtr.Zero, libPathSize, AllocationType.Commit, MemoryProtection.ReadWrite);

                int bytesWritten;
                Imports.WriteProcessMemory(hTarget, pExternLibPath, pLibPath, libPathSize, out bytesWritten);

                IntPtr
                    pLibBase,
                    hMod = CrtWithWait(hTarget, pLoadLib, pExternLibPath);
                if (hMod == IntPtr.Zero)
                {
                    pLibBase = (from ProcessModule module in targetProc.Modules
                                where module.ModuleName.Equals(payloadName)
                                select module)
                        .Single().BaseAddress;
                }
                else
                {
                    pLibBase = hMod;
                }

                var oHost = FindExportRVA(payload, DefaultExport).ToInt32();
                CrtWithWait(hTarget, pLibBase + oHost, pExternLibPath);

                return pLibBase;
            }
            finally
            {
                Marshal.FreeHGlobal(pLibPath);
                Imports.VirtualFreeEx(hTarget, pExternLibPath, 0, AllocationType.Release);
            }
        }

        private static void Eject(IntPtr hTarget, IntPtr pLibBase)
        {
            var hKernel = Imports.GetModuleHandle("kernel32");
            var pFreeLib = Imports.GetProcAddress(hKernel, "FreeLibrary");
            CrtWithWait(hTarget, pFreeLib, pLibBase);
        }

        private static IntPtr CrtWithWait(IntPtr handle, IntPtr pTarget, IntPtr pParam)
        {
            var hThread = IntPtr.Zero;
            try
            {
                hThread = Imports.CreateRemoteThread(handle, IntPtr.Zero, 0, pTarget, pParam, 0, IntPtr.Zero);
                if (Imports.WaitForSingleObject(hThread, (uint)ThreadWaitValue.Infinite) != (uint)ThreadWaitValue.Object0)
                    return IntPtr.Zero;
                //throw new Win32Exception(Marshal.GetLastWin32Error());

                IntPtr hLibModule;
                if (!Imports.GetExitCodeThread(hThread, out hLibModule))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return hLibModule;
            }
            finally
            {
                Imports.CloseHandle(hThread);
            }
        }

        private static IntPtr FindExportRVA(string payload, string export)
        {
            var hModule = IntPtr.Zero;
            try
            {
                hModule = Imports.LoadLibraryEx(payload, IntPtr.Zero, LoadLibraryExFlags.DontResolveDllReferences);
                if (hModule == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var pFunc = Imports.GetProcAddress(hModule, export);
                if (pFunc == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return pFunc - hModule.ToInt32();
            }
            finally
            {
                try
                {
                    Imports.CloseHandle(hModule);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    //SEHException expected. http://stackoverflow.com/questions/9867334/why-is-the-handling-of-exceptions-from-closehandle-different-between-net-4-and
                }
            }
        }
    }
}
