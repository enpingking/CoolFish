using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoolFishNS.Management;
using CoolFishNS.Utilities;
using NLog;

namespace CoolFishNS
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    internal partial class App
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal static void StartUp()
        {
#if DEBUG
            //Debugger.Launch();
#endif
            var app = new App();
            AppDomain.CurrentDomain.UnhandledException += ErrorHandling.UnhandledException;
            TaskScheduler.UnobservedTaskException += ErrorHandling.TaskSchedulerOnUnobservedTaskException;
            Current.DispatcherUnhandledException += ErrorHandling.CurrentOnDispatcherUnhandledException;
            UserPreferences.Default.LoadSettings();
            Utilities.Utilities.InitializeLoggers();
            var window = new MainWindow();
            app.Run(window);
        }

        internal static void ShutDown()
        {
            BotManager.ShutDown();
            UserPreferences.Default.SaveSettings();
            LogManager.Flush(5000);
            LogManager.Shutdown();
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                if (Process.GetCurrentProcess().ProcessName.Contains("CoolFish"))
                {
                    MessageBox.Show("Please start CoolFish.exe instead.");
                    return;
                }
                var thread = new Thread(StartUp);
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Exception thrown in Main", ex);
            }
            ShutDown();
        }
    }
}