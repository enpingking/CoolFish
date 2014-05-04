using System;
using System.Threading;
using CoolFishNS.Bots.FiniteStateMachine;
using CoolFishNS.Management;
using CoolFishNS.Properties;
using CoolFishNS.Utilities;

namespace CoolFishNS.Bots.CoolFishBot
{
    /// <summary>
    ///     Default CoolFish fishing bot that runs the provided IEngine.
    /// </summary>
    public sealed class CoolFishBot : IBot, IDisposable
    {
        private readonly CoolFishEngine _theEngine;

        private Timer _stopTimer;

        private CoolFishBotSettings _window;

        /// <summary>
        ///     Constructor for default CoolFish bot. Assigns the passed IEngine object.
        /// </summary>
        public CoolFishBot()
        {
            _theEngine = new CoolFishEngine();
        }

        /// <inheritdoc />
        public Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }

        /// <inheritdoc />
        public string Name
        {
            get { return "CoolFishBot"; }
        }

        /// <inheritdoc />
        public string Author
        {
            get { return Resources.BotAuthor; }
        }

        /// <inheritdoc />
        public bool IsRunning
        {
            get { return _theEngine.Running; }
        }

        /// <inheritdoc />
        /// <remarks>
        ///     The <see cref="CoolFishNS.Bots.CoolFishBot.CoolFishBot" /> implementation of this method
        ///     does some sanity checking and then starts the <see cref="CoolFishEngine" />
        /// </remarks>
        public void StartBot()
        {
            if (!BotManager.IsAttached)
            {
                Logging.Write("Please attach to a WoW process.");
                return;
            }
            if (!BotManager.LoggedIn)
            {
                Logging.Write("Please log into the game first.");
                return;
            }

            if (LocalSettings.Settings["LootOnlyItems"] &&
                LocalSettings.Settings["DontLootLeft"])
            {
                Logging.Write("You can't \"Loot only items on the left\" and \"Don't loot items on left\" at the same time");
                return;
            }

            if (LocalSettings.Settings["LootQuality"] < 0)
            {
                Logging.Write("Please select a minimum loot quality from the drop down.");
                return;
            }

            if (LocalSettings.Settings["StopOnTime"])
            {
                _stopTimer = new Timer(Callback, null, 0,
                    (int) (LocalSettings.Settings["MinutesToStop"].As<double>()*60*1000));
            }

            LocalSettings.DumpSettingsToLog();
            _theEngine.StartEngine();
        }

        /// <inheritdoc />
        /// <remarks>
        ///     The <see cref="CoolFishNS.Bots.CoolFishBot.CoolFishBot" /> implementation of this method
        ///     Stops the <see cref="CoolFishEngine" />
        /// </remarks>
        public void StopBot()
        {
            _theEngine.StopEngine();
            if (_stopTimer != null)
            {
                _stopTimer.Dispose();
                _stopTimer = null;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        ///     The <see cref="CoolFishNS.Bots.CoolFishBot.CoolFishBot" /> implementation of this method
        ///     opens the <see cref="CoolFishNS.Bots.CoolFishBot.CoolFishBotSettings" /> window
        /// </remarks>
        public void Settings()
        {
            if (_window == null || (!_window.IsActive && !_window.IsVisible))
            {
                _window = new CoolFishBotSettings();
            }
        }


        private void Callback(object state)
        {
            if (IsRunning)
            {
                Logging.Write(Resources.HitTimeLimit);
                StopBot();
            }
        }

        #region IDisposable Members

        /// <summary>
        ///     IDisposable implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _stopTimer != null)
            {
                _stopTimer.Dispose();
            }
        }

        ~CoolFishBot()
        {
            Dispose(false);
        }

        #endregion
    }
}