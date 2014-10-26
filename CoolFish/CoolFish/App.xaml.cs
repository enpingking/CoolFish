using System;
using System.Threading.Tasks;
using System.Windows;
using CoolFishNS.Analytics;
using CoolFishNS.Management;
using CoolFishNS.Properties;
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

        internal static SplashScreen CurrentSplashScreen;

        internal static void StartUp()
        {
            CurrentSplashScreen = new SplashScreen("SplashScreen.png");
            CurrentSplashScreen.Show(false, true);
            AppDomain.CurrentDomain.UnhandledException += ErrorHandling.UnhandledException;
            TaskScheduler.UnobservedTaskException += ErrorHandling.TaskSchedulerOnUnobservedTaskException;
            UserPreferences.Default.LoadSettings();
            Utilities.Utilities.InitializeLoggers();
            MarkedUpAnalytics.Initialize(Settings.Default.apiKey, "CoolFish");
        }

        internal static void ShutDown()
        {
            try
            {
                BotManager.ShutDown();
                UserPreferences.Default.SaveSettings();
                MarkedUpAnalytics.ShutDown("CoolFish");
                LogManager.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Error while shutting down", ex);
            }
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                StartUp();
                new App().Run(new MainWindow());
            }
            catch (Exception ex)
            {
                Logger.Fatal("Exception thrown in Main", ex);
            }
            ShutDown();
        }
    }
}