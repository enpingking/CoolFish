using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CoolFishNS.Bots;
using CoolFishNS.Management;
using CoolFishNS.PluginSystem;
using CoolFishNS.Utilities;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using MessageBox = System.Windows.Forms.MessageBox;

namespace CoolFishNS
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IList<IBot> _bots = new List<IBot>();
        private readonly ICollection<CheckBox> _pluginCheckBoxesList = new Collection<CheckBox>();
        private Process[] _processes;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void UpdateControlSettings()
        {
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


        #region EventHandlers

        private void MetroWindow_Closing_1(object sender, CancelEventArgs e)
        {
            try
            {
                SaveControlSettings();
                BotManager.ShutDown();
                App.Handle.Set();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error closing window",ex);
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
                Logger.ErrorException("Error Starting bot",ex);
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
                Logger.Info("http://unknowndev.github.io/CoolFish/");
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
                Logger.Info(Properties.Resources.PaypalLink);
            }
        }

        private void DonateTab_Click(object sender, RoutedEventArgs e)
        {
            TabControlTC.SelectedItem = DonateTab;
        }

        private void SecretBTN_Click(object sender, RoutedEventArgs e)
        {
            TabControlTC.SelectedItem = MainTab;
            Logger.Info(Properties.Resources.SecretBTNMessage);
        }

        private void MetroWindow_Loaded_1(object sender, RoutedEventArgs e)
        {
            OutputText.Text = Updater.GetNews() + Environment.NewLine;
            Logger.Info("CoolFish Version: " + Utilities.Utilities.Version);
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

            //BotManager.AttachToProcess();

            Updater.Update();
            Updater.StatCount();

            RichTextBoxTarget target = new RichTextBoxTarget
            {
                Name = "RichTextBox",
                Layout = "${time} ${level:uppercase=true} ${message}",
                ControlName = "textbox1",
                FormName = "Form1",
                AutoScroll = true,
                MaxLines = 10000,
               //UseDefaultRowColoringRules = false
            };
              /*target.RowColoringRules.Add(
                  new RichTextBoxRowColoringRule(
                      "level == LogLevel.Trace", // condition
                      "DarkGray", // font color
                      "Control", // background color
                      System.Windows.FontStyle.Regular
                  )
              );
              target.RowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Debug", "Gray", "Control"));
              target.RowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Info", "ControlText", "Control"));
              target.RowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Warn", "DarkRed", "Control"));
              target.RowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Error", "White", "DarkRed", FontStyle.Bold));
              target.RowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Fatal", "Yellow", "DarkRed", FontStyle.Bold));*/

            AsyncTargetWrapper asyncWrapper = new AsyncTargetWrapper(target);
            SimpleConfigurator.ConfigureForTargetLogging(asyncWrapper,LogLevel.Info);


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
                        Logger.ErrorException("An Error occurred trying to configure the plugin: " + plugin.Plugin.Name,ex);
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

        private void OnCloseWindow(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnDragMoveWindow(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OnMinimizeWindow(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

    }
}