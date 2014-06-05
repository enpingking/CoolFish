using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
                    DeleteOldSetupFiles();
                    var client = new GitHubClient(new ProductHeaderValue("CoolFish"));
                    var releases = client.Release.GetAll("unknowndev", "CoolFish").Result;

                    Version latestRelease = Utilities.Version;
                    int latestId = 0;
                    string latestTag = null;
                    foreach (Release release in releases)
                    {
                        var version = new Version(release.TagName.Substring(1) + ".0");
                        if (version > latestRelease)
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
                    Logger.Warn("Failed to check for a new version due to an exception", ex);
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
                client.DownloadFileAsync(new Uri(string.Format("https://github.com/unknowndev/CoolFish/releases/download/{0}/{1}", tag, name)), name, name);
            }
        }

        private static void ClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs asyncCompletedEventArgs)
        {

            if (asyncCompletedEventArgs.Error != null)
            {
                Logger.Error("Error downloading new version", asyncCompletedEventArgs.Error);
                MessageBox.Show("An error occurred while downloading the new version. Please try again or visit the website to download it manually.");
            }
            else
            {
                MessageBox.Show("Download Complete.");
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(asyncCompletedEventArgs.UserState.ToString(), Utilities.ApplicationPath);
                    if (File.Exists(Utilities.ApplicationPath + "\\setup.exe"))
                    {
                        Process.Start("setup.exe");
                    }
                    else
                    {
                        Logger.Warn("Could not find the setup file. Please run it manually.");
                    }
                }
                catch (Exception ex)
                {
                    
                    Logger.Warn("Failed to extract and run the setup. Please run it manually.", ex);
                }
                
            }
        }

        private static void DeleteOldSetupFiles()
        {
            // Clean up old setup files if they exist
            var files = Directory.GetFiles(Utilities.ApplicationPath).Where(file => file.EndsWith(".zip") || file.EndsWith("setup.exe") || file.EndsWith(".msi"));

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}