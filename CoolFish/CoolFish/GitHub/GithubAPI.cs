using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using CoolFishNS.Management.CoolManager.D3D;
using NLog;
using Octokit;
using Octokit.Internal;

namespace CoolFishNS.GitHub
{
    internal static class GithubAPI
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal static GitHubClient Client = new GitHubClient(new ProductHeaderValue("CoolFish"));

        internal static Gist CreateGist(string description, string filename, string contents)
        {
            try
            {
                var gist = new NewGist { Description = description, Public = false };
                gist.Files.Add(filename, contents);
                return Client.Gist.Create(gist).Result;
            }
            catch (RateLimitExceededException ex)
            {
                Logger.Warn("Failed to create gist due to exceeding hourly requests. " + ex.Reset, ex);
            }
            catch (Exception ex)
            {
                
                Logger.Warn("Failed to create Gist", ex);
                
            }
            return null;

        }

        internal static Tuple<int, string> GetLatestVersionInfo()
        {
            var latestId = -1;
            var latestTag = (string) null;
            try
            {
                var releases = Client.Release.GetAll("unknowndev", "CoolFish").Result;
                var latestRelease = Utilities.Utilities.Version;
                
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


            }
            catch (RateLimitExceededException ex)
            {
                Logger.Warn("Failed to check for a new version due to exceeding hourly requests. " + ex.Reset, ex);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to check for new version", ex);
            }
            return latestId > -1 ? new Tuple<int, string>(latestId, latestTag) : null;
        }

        internal static void DownloadAsset(int id, string tag)
        {
            var assets = Client.Release.GetAssets("unknowndev", "CoolFish", id).Result;
            if (assets.Any())
            {
                using (var client = new WebClient())
                {
                    Logger.Info("Downloading File...");
                    DeleteOldSetupFiles();
                    client.DownloadFileCompleted += ClientOnDownloadFileCompleted;
                    client.DownloadFileAsync(new Uri(string.Format("https://github.com/unknowndev/CoolFish/releases/download/{0}/{1}", tag, assets[0].Name)), assets[0].Name, assets[0].Name);
                }
            }
        }

        private static void ClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs asyncCompletedEventArgs)
        {

            if (asyncCompletedEventArgs.Error != null)
            {
                Logger.Warn("Error downloading new version", asyncCompletedEventArgs.Error);
                MessageBox.Show("An error occurred while downloading the new version. Please try again or visit the website to download it manually.");
            }
            else
            {
                
                try
                {
                    MessageBox.Show("Download Complete.");
                    System.IO.Compression.ZipFile.ExtractToDirectory(asyncCompletedEventArgs.UserState.ToString(), Utilities.Utilities.ApplicationPath);
                    if (File.Exists(Utilities.Utilities.ApplicationPath + "\\setup.exe"))
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
            try
            {
                // Clean up old setup files if they exist
                var files = Directory.GetFiles(Utilities.Utilities.ApplicationPath).Where(file => file.EndsWith(".zip") || file.EndsWith("setup.exe") || file.EndsWith(".msi"));

                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                
                Logger.Warn("Failed to delete old setup files", ex);
            }
            
        }

    }
}
