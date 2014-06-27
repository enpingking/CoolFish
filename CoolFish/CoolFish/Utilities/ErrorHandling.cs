using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoolFishNS.GitHub;
using NLog;

namespace CoolFishNS.Utilities
{
    internal static class ErrorHandling
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            Logger.Error("Unhandled error has occurred on another thread. This may cause an unstable state of the application.", (Exception)unobservedTaskExceptionEventArgs.Exception);
        }

        internal static void UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            try
            {
                var e = (Exception)ex.ExceptionObject;
                var gist = GithubAPI.CreateGist("UnhandledException", "LogFile", File.ReadAllText(App.ActiveLogFileName));
                string msg;
                if (gist != null)
                {
                    msg = "An unhandled error has occurred. A Gist has been uploaded with your log file to assist with debugging: " + gist.HtmlUrl +
                          " The application will now exit.";
                }
                else
                {
                    msg = "An unhandled error has occurred. Please send the log file to the developer. The application will now exit.";
                }
                Logger.Fatal(msg, e);
                MessageBox.Show(msg);
                App.ShutDown();
                Environment.Exit(-1);

            }
            catch (Exception exception)
            {
                Logger.Fatal("Failed to handle exception", exception);
            }


        }

    }
}
