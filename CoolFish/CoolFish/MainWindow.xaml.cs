using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CoolFishNS.Bots;
using CoolFishNS.Management;
using CoolFishNS.PluginSystem;
using CoolFishNS.Utilities;
using MessageBox = System.Windows.Forms.MessageBox;

namespace CoolFishNS
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow
    {
        private readonly IList<IBot> _bots = new List<IBot>();
        private readonly ICollection<CheckBox> _pluginCheckBoxesList = new Collection<CheckBox>();
        private Process[] _processes;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateControlSettings()
        {
            if (LocalSettings.Settings["BlackBackground"])
            {
                BackgroundColorObj.Color = Color.FromArgb(0xFF, 0x0, 0x0, 0x0);
            }


            foreach (var plugin in PluginManager.Plugins)
            {
                var cb = new CheckBox {Content = plugin.Key};
                cb.Checked += changedCheck;
                cb.Unchecked += changedCheck;
                cb.IsChecked = LocalSettings.Plugins.ContainsKey(plugin.Key) && (LocalSettings.Plugins[plugin.Key].isEnabled == true);

                _pluginCheckBoxesList.Add(cb);
            }


            ScriptsLB.ItemsSource = _pluginCheckBoxesList;
        }

        private void SaveControlSettings()
        {
            foreach (CheckBox script in _pluginCheckBoxesList)
            {
                LocalSettings.Plugins[script.Content.ToString()] = new SerializablePlugin
                {
                    fileName = script.Content.ToString(),
                    isEnabled = script.IsChecked
                };
            }
        }

        private void RefreshProcesses()
        {
            ProcessCB.Items.Clear();

            _processes = GetWowProcesses();

            foreach (Process process in _processes)
            {
                try
                {
                    ProcessCB.Items.Add("Id: " + process.Id + " Name: " + process.MainWindowTitle);
                }
                catch (Exception ex)
                {
                    Logging.Log(ex);
                }
            }
        }

        /// <summary>
        ///     Gets a List of 32-bit Wow processes currently running on the system
        /// </summary>
        /// <returns>List of Process objects</returns>
        public static Process[] GetWowProcesses()
        {
            Process[] proc = Process.GetProcessesByName("WoW");
            Process[] proc64Bit = Process.GetProcessesByName("WoW-64");

            if (!proc.Any())
            {
                if (proc64Bit.Any())
                {
                    Logging.Write(
                        "It seems you are running a 64bit version of WoW. CoolFish does not support that version. Please start the 32bit version instead.");
                }
                else
                {
                    Logging.Write("No WoW processes were found.");
                }
            }


            return proc;
        }

        private void AppendMessage(object sender, MessageEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (OutputText.Text.Length > 5000)
                    {
                        OutputText.Text = string.Empty;
                    }
                    OutputText.Text += Log.TimeStamp + " " + args.Message + Environment.NewLine;
                    OutputText.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    Logging.Log("Exception while writing: \n" + ex);
                }
            }, DispatcherPriority.Background);
        }

        #region EventHandlers

        private void btn_Attach_Click(object sender, EventArgs e)
        {
            if (_processes.Length > ProcessCB.SelectedIndex && ProcessCB.SelectedIndex >= 0) // return if we have an invalid process
            {
                BotManager.AttachToProcess(_processes[ProcessCB.SelectedIndex]);
            }
            else
            {
                Logging.Write("Please pick a process to attach to.");
            }
        }

        private void ComboBox_DropDownOpened_1(object sender, EventArgs e)
        {
            try
            {
                RefreshProcesses();
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }

        private void MetroWindow_Closing_1(object sender, CancelEventArgs e)
        {
            try
            {
                SaveControlSettings();
                BotManager.ShutDown();
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }

        private void StartBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveControlSettings();
                LocalSettings.SaveSettings();
                BotManager.StartActiveBot();
            }
            catch (Exception ex)
            {
                Logging.Write(ex.ToString());
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            BotManager.StopActiveBot();
        }

        private void HelpBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://unknowndev.github.io/CoolFish/");
            }
            catch (Exception ex)
            {
                TabControlTC.SelectedItem = MainTab;
                Logging.Log(ex);
                Logging.Write("http://unknowndev.github.io/CoolFish/");
            }
        }

        private void UpdateBTN_Click(object sender, RoutedEventArgs e)
        {
            TabControlTC.SelectedItem = MainTab;
            Updater.Update();
        }

        private void MainTab_Click(object sender, RoutedEventArgs e)
        {
            TabControlTC.SelectedItem = MainTab;
        }

        private void DonateBTN_Click(object sender, MouseButtonEventArgs e)
        {
            TabControlTC.SelectedItem = MainTab;
            try
            {
                Process.Start(Properties.Resources.PaypalLink);
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
                Logging.Write(Properties.Resources.PaypalLink);
            }
        }

        private void DonateTab_Click(object sender, RoutedEventArgs e)
        {
            TabControlTC.SelectedItem = DonateTab;
        }

        private void SecretBTN_Click(object sender, RoutedEventArgs e)
        {
            TabControlTC.SelectedItem = MainTab;


            if (BackgroundColorObj.Color == Color.FromArgb(0xFF, 0x34, 0xBF, 0xF3))
            {
                BackgroundColorObj.Color = Color.FromArgb(0xFF, 0x0, 0x0, 0x0);
                LocalSettings.Settings["BlackBackground"] = BotSetting.As(true);
            }
            else
            {
                BackgroundColorObj.Color = Color.FromArgb(0xFF, 0x34, 0xBF, 0xF3);
                LocalSettings.Settings["BlackBackground"] = BotSetting.As(false);
            }


            Logging.Write(Properties.Resources.SecretBTNMessage);
        }

        private void MetroWindow_Loaded_1(object sender, RoutedEventArgs e)
        {
            Logging.OnWrite += AppendMessage;
            OutputText.Text = Updater.GetNews() + Environment.NewLine;
            Logging.Log("CoolFish Version: " + Utilities.Utilities.Version);

            BotManager.StartUp();

            UpdateControlSettings();

            if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1)
            {
                FontSize = 3;
                BackgroundColorObj.Color = Color.FromArgb(0xFF, 0xFF, 0x5C, 0xCD);
                GradientStopObj.Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
                MessageBox.Show("Happy April 1st! :)");
            }

            BotBaseCB_DropDownOpened(null, null);
            BotBaseCB.SelectedIndex = 0;

            _processes = GetWowProcesses();
            if (_processes.Length == 1)
            {
                BotManager.AttachToProcess(_processes[0]);
            }

            Updater.Update();
            Updater.StatCount();
        }


        private void PluginsBTN_Click(object sender, RoutedEventArgs e)
        {
            TabControlTC.SelectedItem = PluginTab;
            ScriptsLB_SelectionChanged(sender, null);
        }

        private void changedCheck(object sender, RoutedEventArgs routedEventArgs)
        {
            var box = (CheckBox) sender;

            PluginManager.Plugins[box.Content.ToString()].Enabled = box.IsChecked == true;
        }

        private void ConfigBTN_Click(object sender, RoutedEventArgs e)
        {
            object item = ScriptsLB.SelectedItem;

            if (item != null)
            {
                var cb = (CheckBox) item;

                PluginContainer plugin = PluginManager.Plugins.ContainsKey(cb.Content.ToString())
                    ? PluginManager.Plugins[cb.Content.ToString()]
                    : null;

                if (plugin != null)
                {
                    try
                    {
                        plugin.Plugin.OnConfig();
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(ex);
                        Logging.Write("An Error occurred trying to configure the plugin: " + plugin.Plugin.Name);
                    }
                }
            }
        }

        private void ScriptsLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object value = ScriptsLB.SelectedItem;

            if (value != null)
            {
                var cb = (CheckBox) value;
                IPlugin p = PluginManager.Plugins[cb.Content.ToString()].Plugin;

                DescriptionBox.Text = p.Description;
                AuthorTB.Text = "Author: " + p.Author;
                VersionTB.Text = "Version: " + p.Version;
            }
        }

        private void MinimizeBtnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            BotManager.Settings();
        }

        private void BotBaseCB_DropDownOpened(object sender, EventArgs e)
        {
            BotBaseCB.Items.Clear();
            _bots.Clear();
            foreach (var pair in BotManager.LoadedBots)
            {
                BotBaseCB.Items.Add(pair.Value.Name + " (" + pair.Value.Version + ") by " +
                                    pair.Value.Author);
                _bots.Add(pair.Value);
            }
        }

        private void BotBaseCB_DropDownClosed(object sender, EventArgs e)
        {
            if (BotBaseCB.SelectedIndex == -1)
            {
                BotBaseCB.SelectedIndex = 0;
            }

            BotManager.SetActiveBot(_bots[BotBaseCB.SelectedIndex]);
        }

        #endregion
    }
}