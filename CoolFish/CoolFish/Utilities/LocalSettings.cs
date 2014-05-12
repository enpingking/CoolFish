using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NLog;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     Settings class in order to save the settings I wish to track about users
    /// </summary>
    internal static class LocalSettings
    {
        internal static Collection<SerializableItem> Items = new Collection<SerializableItem>();

        internal static Dictionary<string, SerializablePlugin> Plugins = new Dictionary<string, SerializablePlugin>();

        internal static Dictionary<string, BotSetting> Settings = new Dictionary<string, BotSetting>();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Loads default CoolFish settings
        /// </summary>
        internal static void LoadDefaults()
        {
            Logger.Info("Loading Default Settings.");
            LoadDefaultSettings();
            SaveSettings();
        }

        private static void LoadDefaultSettings()
        {
            Settings = new Dictionary<string, BotSetting>
            {
                {"NoLure", new BotSetting {Value = false}},
                {"LootOnlyItems", new BotSetting {Value = false}},
                {"StopOnNoLures", new BotSetting {Value = false}},
                {"StopOnBagsFull", new BotSetting {Value = false}},
                {"LogoutOnStop", new BotSetting {Value = false}},
                {"UseRaft", new BotSetting {Value = false}},
                {"UseCharm", new BotSetting {Value = false}},
                {"ShutdownPConStop", new BotSetting {Value = false}},
                {"DontLootLeft", new BotSetting {Value = false}},
                {"MinutesToStop", new BotSetting {Value = 0}},
                {"LootQuality", new BotSetting {Value = -1}},
                {"SoundOnWhisper", new BotSetting {Value = false}},
                {"UseRumsey", new BotSetting {Value = false}},
                {"UseSpear", new BotSetting {Value = false}},
                {"LogLevel", new BotSetting {Value = 2}},
                {"LanguageIndex", new BotSetting {Value = 0}},
                {"CloseWoWonStop", new BotSetting {Value = false}},
                {"StopOnTime", new BotSetting {Value = false}},
                {"DoFishing", new BotSetting {Value = true}},
                {"DoBobbing", new BotSetting {Value = true}},
                {"DoLoot", new BotSetting {Value = true}}
            };
            Items = new Collection<SerializableItem>();
            Plugins = new Dictionary<string, SerializablePlugin>();
        }

        /// <summary>
        ///     Prints all of the settings as they currently are to the log file.
        /// </summary>
        internal static void DumpSettingsToLog()
        {
            Logger.Debug("----Settings----");
            foreach (var botSetting in Settings)
            {
                Logger.Debug(botSetting.Key + ": " + botSetting.Value);
            }
            Logger.Debug("----Plugins----");

            foreach (var serializablePlugin in Plugins)
            {
                Logger.Debug(serializablePlugin.Key + " - Enabled: " + serializablePlugin.Value.isEnabled);
            }
            Logger.Debug("----Items----");
            foreach (SerializableItem serializableItem in Items)
            {
                Logger.Debug(serializableItem.Value);
            }
        }


        /// <summary>
        ///     Use XML seralization to save settings
        /// </summary>
        internal static void SaveSettings()
        {
            Serializer.Serialize("Settings.dat", Settings);
            Serializer.Serialize("Plugins.dat", Plugins);
            Serializer.Serialize("Items.dat", Items);
        }

        /// <summary>
        ///     Use seralization to load settings
        /// </summary>
        internal static void LoadSettings()
        {
            try
            {
                LoadDefaultSettings();
                Settings.Upsert(Serializer.DeSerialize<Dictionary<string, BotSetting>>("Settings.dat"));
                Plugins.Upsert(Serializer.DeSerialize<Dictionary<string, SerializablePlugin>>("Plugins.dat"));
                Items = Serializer.DeSerialize<Collection<SerializableItem>>("Items.dat");
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error loading settings", ex);
                LoadDefaults();
            }
        }
    }
}