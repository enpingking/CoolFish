using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoolFishNS.Management;
using CoolFishNS.Utilities;

namespace CoolFishNS
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    internal partial class App
    {
        static App()
        {
            // Change culture under which this application runs
            var ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            Logging.Write("Unhandled error has occurred on another thread. This may cause an unstable state of the application.");
            Logging.Log(unobservedTaskExceptionEventArgs.Exception);
        }

        public static void UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            try
            {
                var e = (Exception) ex.ExceptionObject;
                Logging.Log(e.ToString());
                MessageBox.Show("An unhandled Error has occurred. Please send the log file to the developer. The application will now exit.");
                Logging.Flush();
                BotManager.ShutDown();
            }
            catch
            {
            }


            Environment.Exit(-1);
        }
    }
}