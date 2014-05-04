using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using System.Windows.Forms;
using Octokit;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     All Web Request related functions are in this class (Updating, ShouldRun, News).
    /// </summary>
    internal static class Updater
    {
        internal static void StatCount()
        {
            using (var wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.UserAgent] = "CoolFish";
                wc.DownloadDataAsync(new Uri("http://c.statcounter.com/9756717/0/323389f4/1/"));
            }
        }

        internal static string GetNews()
        {
            try
            {
                WebRequest request = WebRequest.Create("http://unknowndev.github.io/CoolFish/Message.txt");

                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Reload);
                WebResponse response = request.GetResponse();
                Stream stream = response.GetResponseStream();
                if (stream != null)
                {
                    var reader = new StreamReader(stream);

                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Logging.Write("Could not connect to news feed. Website is down?");
                Logging.Log(ex);
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
                        Logging.Write("No new versions available.");
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(ex);
                }
            });
        }

        private static void DownloadNewVersion(string tag, string name)
        {
            using (var client = new WebClient())
            {
                Logging.Write("Downloading File...");
                client.DownloadFileCompleted += ClientOnDownloadFileCompleted;
                client.DownloadFileAsync(new Uri(string.Format("https://github.com/unknowndev/CoolFish/releases/download/{0}/{1}", tag, name)), name);
            }
        }

        private static void ClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs asyncCompletedEventArgs)
        {
            if (asyncCompletedEventArgs.Error != null)
            {
                Logging.Log(asyncCompletedEventArgs.Error);
                MessageBox.Show("An error occurred while downloading the new version. Please try again or visit the website to download it manually.");
            }
            else
            {
                MessageBox.Show("Download Complete. Close Coolfish and extract the downloaded zip file.");
            }
        }
    }
}