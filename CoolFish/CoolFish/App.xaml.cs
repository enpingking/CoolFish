using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoolFishNS.Management;
using CoolFishNS.Targets;
using CoolFishNS.Utilities;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace CoolFishNS
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    internal partial class App
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        internal static App CurrentApp = new App();

        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            Logger.Error("Unhandled error has occurred on another thread. This may cause an unstable state of the application.", (Exception)unobservedTaskExceptionEventArgs.Exception);
        }

        public static void UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            try
            {
                var e = (Exception) ex.ExceptionObject;
                Logger.Fatal("An unhandled error has occurred. Please send the log file to the developer. The application will now exit.", e);
                MessageBox.Show("An unhandled error has occurred. Please send the log file to the developer. The application will now exit.");
                ShutDown();
                
            }
            catch (Exception)
            {
            }
            
            Environment.Exit(-1);
        }

        internal static void StartUp()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
                CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
                CultureInfo.DefaultThreadCurrentCulture = culture;
                LocalSettings.LoadSettings();
                InitializeLoggers();
                
            }
            catch (Exception ex)
            {
                Logger.Error("Error while starting up", ex);
            }

        }

        internal static void ShutDown()
        {
            try
            {
                BotManager.ShutDown();
                LocalSettings.SaveSettings();
                Analytics.MarkedUp.ShutDown();
                LogManager.Flush(5000);
                LogManager.Shutdown();
            }
            catch (Exception)
            {
                
            }
        }

        private static void InitializeLoggers()
        {
            var config = new LoggingConfiguration();
            var now = DateTime.Now;
            var directory = string.Format("{0}\\Logs\\{1}", Utilities.Utilities.ApplicationPath, now.ToString("MMMM dd yyyy"));
            const string layout = @"[${date:format=MM/dd/yy h\:mm\:ss.ffff tt}] [${level:uppercase=true}] ${message} ${onexception:inner=${newline}${exception:format=tostring}}";
            var file = new FileTarget
            {
                FileName = string.Format("{0}\\[CoolFish-{1}] {2}.txt",directory,Process.GetCurrentProcess().Id,now.ToString("T").Replace(':','.')),
                Layout = layout,
                CreateDirs = true,
                ConcurrentWrites = false
                    
            };

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace,
                new AsyncTargetWrapper(file) {OverflowAction = AsyncTargetWrapperOverflowAction.Grow}));

            var markedUp = new MarkedUpTarget
            {
                Layout = layout
            };

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace,
                new AsyncTargetWrapper(markedUp) { OverflowAction = AsyncTargetWrapperOverflowAction.Grow }));


            LogManager.Configuration = config;
        }

        [STAThread]
        public static void Main()
        {
            StartUp();
            CurrentApp.Run(new MainWindow());
            ShutDown();
        }
    }
}