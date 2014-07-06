using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CoolFishNS.Management;
using CoolFishNS.Utilities;
using NLog;
using MessageBox = System.Windows.Forms.MessageBox;

namespace CoolFishNS.Bots.CoolFishBot
{
    /// <summary>
    ///     Interaction logic for CoolFishBotSettings.xaml
    /// </summary>
    internal partial class CoolFishBotSettings
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Collection<SerializableItem> _items = new Collection<SerializableItem>();

        public CoolFishBotSettings()
        {
            InitializeComponent();
        }


        private void StopTimeMinutesTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            double minutes;
            if (double.TryParse(StopTimeMinutesTB.Text, out minutes))
            {
                try
                {
                    DateTime date = DateTime.Now.AddMinutes(minutes);

                    DateLBL.Content = date.ToString();
                }
                catch (Exception)
                {
                    StopTimeMinutesTB.Text = string.Empty;
                    DateLBL.Content = "Invalid Date";
                }
                
            }
            else
            {
                StopTimeMinutesTB.Text = string.Empty;
                DateLBL.Content = "Invalid Date";
            }
        }

        private void CheckDataGrid()
        {

            IEditableCollectionView collection = ItemsGrid.Items;
           
            if (collection.IsEditingItem)
            {
                collection.CommitEdit();
            }
            if (collection.IsAddingNew)
            {
                collection.CommitNew();
                
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            CheckDataGrid();
            if (!BotManager.ActiveBot.IsRunning)
            {
                SaveControlSettings();
                LocalSettings.SaveSettings();
                Logger.Info("CoolFishBot settings saved.");
                Close();
            }
            else
            {
                MessageBox.Show("Can't save settings while the bot is running. Please stop the bot first.");
            }
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            CheckDataGrid();
            Close();
        }


        public void FillDataGrid()
        {
            try
            {
                ItemsGrid.ItemsSource = null;
                ItemsGrid.ItemsSource = _items;
            }
            catch (InvalidOperationException)
            {
                // Shouldn't happen anymore, but in case it does silently fail since there is no impact
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating datagrid", ex);
            }
            
        }

        private void UpdateControlSettings()
        {
            NoLureCB.IsChecked = LocalSettings.Settings["NoLure"];
            LootOnlyItemsCB.IsChecked = LocalSettings.Settings["LootOnlyItems"];
            StopTimeMinutesTB.Text = LocalSettings.Settings["MinutesToStop"].ToString();
            LogoutCB.IsChecked = LocalSettings.Settings["LogoutOnStop"];
            UseRaftCB.IsChecked = LocalSettings.Settings["UseRaft"];
            StopTimeCB.IsChecked = LocalSettings.Settings["StopOnTime"];
            StopNoLuresCB.IsChecked = LocalSettings.Settings["StopOnNoLures"];
            StopFullBagsCB.IsChecked = LocalSettings.Settings["StopOnBagsFull"];
            CloseAppsCB.IsChecked = LocalSettings.Settings["CloseWoWonStop"];
            ShutdownCB.IsChecked = LocalSettings.Settings["ShutdownPConStop"];
            DontLootCB.IsChecked = LocalSettings.Settings["DontLootLeft"];
            QualityCMB.SelectedIndex = LocalSettings.Settings["LootQuality"] + 1;
            SoundWhisperCB.IsChecked = LocalSettings.Settings["SoundOnWhisper"];
            UseCharmCB.IsChecked = LocalSettings.Settings["UseCharm"];
            UseRumseyCB.IsChecked = LocalSettings.Settings["UseRumsey"];
            UseSpearCB.IsChecked = LocalSettings.Settings["UseSpear"];
            CastFishingCB.IsChecked = LocalSettings.Settings["DoFishing"];
            ClickBobberCB.IsChecked = LocalSettings.Settings["DoBobbing"];
            DoLootingCB.IsChecked = LocalSettings.Settings["DoLoot"];
            _items = LocalSettings.Items;
            FillDataGrid();
        }

        private void SaveControlSettings()
        {
            LocalSettings.Settings["NoLure"] = NoLureCB.IsChecked;
            LocalSettings.Settings["LootOnlyItems"] = LootOnlyItemsCB.IsChecked;
            LocalSettings.Settings["LogoutOnStop"] = LogoutCB.IsChecked;
            LocalSettings.Settings["UseRaft"] = UseRaftCB.IsChecked;
            LocalSettings.Settings["StopOnTime"] = StopTimeCB.IsChecked;
            LocalSettings.Settings["StopOnNoLures"] = StopNoLuresCB.IsChecked;
            LocalSettings.Settings["StopOnBagsFull"] = StopFullBagsCB.IsChecked;
            LocalSettings.Settings["CloseWoWonStop"] = CloseAppsCB.IsChecked;
            LocalSettings.Settings["ShutdownPConStop"] = ShutdownCB.IsChecked;
            LocalSettings.Settings["DontLootLeft"] = DontLootCB.IsChecked;
            LocalSettings.Settings["LootQuality"] = QualityCMB.SelectedIndex - 1;
            LocalSettings.Settings["SoundOnWhisper"] = SoundWhisperCB.IsChecked;
            LocalSettings.Settings["UseCharm"] = UseCharmCB.IsChecked;
            LocalSettings.Settings["UseRumsey"] = UseRumseyCB.IsChecked;
            LocalSettings.Settings["UseSpear"] = UseSpearCB.IsChecked;
            LocalSettings.Settings["DoFishing"] = CastFishingCB.IsChecked;
            LocalSettings.Settings["DoBobbing"] = ClickBobberCB.IsChecked;
            LocalSettings.Settings["DoLoot"] = DoLootingCB.IsChecked;
            LocalSettings.Items = _items;
            double result;
            if (double.TryParse(StopTimeMinutesTB.Text, out result))
            {
                LocalSettings.Settings["MinutesToStop"] = result;
            }
            else
            {
                Logger.Warn("Invalid Stop Time.");
                LocalSettings.Settings["MinutesToStop"] = 0d;
            }
            
        }


        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var col1 = new DataGridTextColumn { Binding = new Binding("Value"), Header = "ItemId or Name", Width = 150 };
                col1.SetValue(NameProperty, "ItemColumn");

                ItemsGrid.Columns.Add(col1);
                UpdateControlSettings();
            }
            catch (Exception ex)
            {
                
                Logger.Error("Error thrown while updating controls", ex);
            }
            
        }
    }
}