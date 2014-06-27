using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoolFishNS.Management;
using CoolFishNS.Targets;
using CoolFishNS.Utilities;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Octokit;

namespace CoolFishNS
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    internal partial class App
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal static App CurrentApp = new App();
        internal static string ActiveLogFileName;

        internal static void StartUp()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += ErrorHandling.UnhandledException;
                TaskScheduler.UnobservedTaskException += ErrorHandling.TaskSchedulerOnUnobservedTaskException;
                CultureInfo.DefaultThreadCurrentCulture = DefaultCultureInfo();
                LogManager.DefaultCultureInfo = DefaultCultureInfo;
                LocalSettings.LoadSettings();
                InitializeLoggers();            
                
            }
            catch (Exception ex)
            {
                Logger.Fatal("Error while starting up", ex);
            }
           


        }

        private static CultureInfo DefaultCultureInfo()
        {
            return CultureInfo.CreateSpecificCulture("en-US");
        }

        internal static void ShutDown()
        {
            try
            {
                BotManager.ShutDown();
                LocalSettings.SaveSettings();
                Analytics.MarkedUpAnalytics.ShutDown();
                LogManager.Flush(5000);
                LogManager.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Error while shutting down", ex);
            }
        }

        private static void InitializeLoggers()
        {
            var config = new LoggingConfiguration();
            var now = DateTime.Now;
            ActiveLogFileName = string.Format("{0}\\Logs\\{1}\\[CoolFish-{2}] {3}.txt", Utilities.Utilities.ApplicationPath, now.ToString("MMMM dd yyyy"), Process.GetCurrentProcess().Id,
                now.ToString("T").Replace(':', '.'));

            var file = new FileTarget
            {
                FileName = ActiveLogFileName,
                Layout = @"[${date:format=MM/dd/yy h\:mm\:ss.ffff tt}] [${level:uppercase=true}] ${message} ${onexception:inner=${newline}${exception:format=tostring}}",
                CreateDirs = true,
                ConcurrentWrites = false
                    
            };

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace,
                new AsyncTargetWrapper(file) {OverflowAction = AsyncTargetWrapperOverflowAction.Grow}));

            var markedUp = new MarkedUpTarget
            {
                Layout = @"[${date:format=MM/dd/yy h\:mm\:ss.ffff tt}] [${level:uppercase=true}] ${message} ${onexception:inner=${newline}${exception:format=tostring}}"
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