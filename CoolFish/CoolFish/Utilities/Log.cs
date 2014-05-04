using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CoolFishNS.Utilities
{
    internal static class Log
    {
        private static string _fileNameDate;
        private static int _id;

        internal static string TimeStamp
        {
            get { return string.Format("[{0}] ", DateTime.Now.ToLongTimeString()); }
        }

        internal static void StartUp()
        {
            _fileNameDate = DateTime.Now.ToShortDateString().Replace("/", "-") + " " + TimeStamp.Replace(":", ".");
            _id = Process.GetCurrentProcess().Id;
            if (!Directory.Exists(Utilities.ApplicationPath + "\\Logs"))
            {
                Directory.CreateDirectory(Utilities.ApplicationPath + "\\Logs");
            }
            Logging.OnLog += LogMessage;
            Logging.OnWrite += LogMessage;
        }

        internal static void LogMessage(object sender, MessageEventArgs eventArgs)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                using (TextWriter tw =
                    new StreamWriter(
                        String.Format("{0}\\Logs\\[CoolFish-{1}] {2} Log.txt", Utilities.ApplicationPath, _id, _fileNameDate), true))
                {
                    tw.WriteLine(TimeStamp + " " + eventArgs.Message);
                }
            }, DispatcherPriority.Background);
        }
    }
}