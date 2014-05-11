using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CoolFishNS.Management;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using FontStyle = System.Drawing.FontStyle;

namespace CoolFishNS
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        internal static ManualResetEvent Handle = new ManualResetEvent(false);

        public App()
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
            Logger.ErrorException("Unhandled error has occurred on another thread. This may cause an unstable state of the application.",
                unobservedTaskExceptionEventArgs.Exception);
        }

        public static void UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            try
            {
                var e = (Exception) ex.ExceptionObject;
                Logger.FatalException("An unhandled error has occurred. Please send the log file to the developer. The application will now exit.", e);
                BotManager.ShutDown();
                Handle.Set();
               
            }
            catch
            {
            }

            Environment.Exit(-1);
        }

        private static void InitializeLoggers()
        {
            var config = new LoggingConfiguration();
            var now = DateTime.Now;
            var file = new FileTarget
            {
                FileName = string.Format("{0}\\Logs\\{1}\\[CoolFish-{2}] {3}.txt", Utilities.Utilities.ApplicationPath, now.ToString("MMMM dd yyyy"), Process.GetCurrentProcess().Id,
                    now.ToString("T").Replace(":",".")),
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${onexception:inner=${newline}${exception:format=tostring}}"
            };

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Fatal, new MessageBoxTarget{Caption = "Error", Layout = "${message}"}));
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, new AsyncTargetWrapper(file) {OverflowAction = AsyncTargetWrapperOverflowAction.Grow}));


            LogManager.Configuration = config;
        }

        [STAThread]
        public static void Main()
        {
            InitializeLoggers();
            var appthread = new Thread(() =>
            {
                var app = new App();
                app.InitializeComponent();
                app.Run(new MainWindow());
            });
            appthread.SetApartmentState(ApartmentState.STA);
            appthread.Start();
            Handle.WaitOne();
        }
    }
}