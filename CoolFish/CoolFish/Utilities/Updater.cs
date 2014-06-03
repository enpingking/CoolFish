using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using Octokit;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     All Web Request related functions are in this class (Updating, ShouldRun, News).
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
                    return client.DownloadString("http://unknowndev.github.io/CoolFish/Message.txt"))
                }
                
                
            }
            catch (Exception ex)
            {
                Logger.WarnException("Could not connect to news feed. Website is down?", ex);
            }
            return string.Empty;
        }

        internal static void Update()
        {
            Task.Run(() =>
            {
                try
                {
                    var client = new GitHubClient(new ProductHeaderValue("CoolFish"));
                    IReadOnlyList<Release> releases = client.Release.GetAll("unknowndev", "CoolFish").Result;

                    Version latestRelease = Utilities.Version;
                    int latestId = 0;
                    string latestTag = null;
                    foreach (Release release in releases)
                    {
                        var version = new Version(release.TagName.Substring(1) + ".0");
                        if (version > latestRelease && !release.Prerelease)
                        {
                            latestTag = release.TagName;
                            latestId = release.Id;
                            latestRelease = version;
                        }
                    }
                    if (latestTag != null)
                    {
                        DialogResult result =
                            MessageBox.Show("There is a new version of CoolFish available. Would you like to download it now?",
                                "New Version", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            IReadOnlyList<ReleaseAsset> assets = client.Release.GetAssets("unknowndev", "CoolFish", latestId).Result;
                            if (assets.Any())
                            {
                                DownloadNewVersion(latestTag, assets[0].Name);
                            }
                        }
                    }
                    else
                    {
                        Logger.Info("No new versions available.");
                    }
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.SessionEvent(ex.GetType() + " : " + ex.Message);
                }
            });
        }

        private static void DownloadNewVersion(string tag, string name)
        {
            using (var client = new WebClient())
            {
                Logger.Info("Downloading File...");
                client.DownloadFileCompleted += ClientOnDownloadFileCompleted;
                client.DownloadFileAsync(new Uri(string.Format("https://github.com/unknowndev/CoolFish/releases/download/{0}/{1}", tag, name)), name);
            }
        }

        private static void ClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs asyncCompletedEventArgs)
        {
            if (asyncCompletedEventArgs.Error != null)
            {
                Logger.ErrorException("Error downloading new version", asyncCompletedEventArgs.Error);
                MessageBox.Show("An error occurred while downloading the new version. Please try again or visit the website to download it manually.");
            }
            else
            {
                MessageBox.Show("Download Complete. Close Coolfish and extract the downloaded zip file.");
            }
        }
    }
}