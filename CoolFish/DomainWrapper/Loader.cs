using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using DllExporter;

namespace DomainWrapper
{
    public static class Loader
    {
        private const string AppName = "CoolFishLib.exe";

        [DllExport]
        [STAThread]
        public static void Host([MarshalAs(UnmanagedType.LPWStr)]string loadDir)
        {
#if LAUNCH_MDA
            System.Diagnostics.Debugger.Launch();
#endif
            loadDir = Path.GetDirectoryName(loadDir);
            Trace.Assert(Directory.Exists(loadDir));

            Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(loadDir, @"Logs\", AppName + ".Loader.log")));

            try
            {
                using (var host = new PathedDomainHost(AppName, loadDir))
                {
                    host.Execute();
                }

            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
    }
}