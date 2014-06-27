using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoolFishNS.GitHub;
using NLog;
using Octokit;


namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     All Web Request related functions are in this class (Updater and News).
    /// </summary>
    internal static class Updater
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal static string GetNews()
        {
            try
            {
                
                using (var client = new WebClient {Proxy = WebRequest.DefaultWebProxy})
                {
                    return client.DownloadString("http://unknowndev.github.io/CoolFish/Message.txt");
                }
                
                
            }
            catch (Exception ex)
            {
                Logger.Warn("Could not connect to news feed. Website is down?", ex);
            }
            return string.Empty;
        }

        internal static void Update()
        {
            Task.Run(() =>
            {
                try
                {
                    var latestInfo = GithubAPI.GetLatestVersionInfo();
                    if (latestInfo != null)
                    {
                        var result =
                            MessageBox.Show("There is a new version of CoolFish available. Would you like to download it now?",
                                "New Version", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            GithubAPI.DownloadAsset(latestInfo.Item1, latestInfo.Item2);
                        }
                    }
                    else
                    {
                        Logger.Info("No new versions available.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to check for a new version due to an exception", ex);
                }
            });
        }


    }
}