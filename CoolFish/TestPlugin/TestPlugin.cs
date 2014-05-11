using System;
using CoolFishNS.PluginSystem;
using CoolFishNS.Utilities;


namespace TestPlugin
{
    public class TestPlugin : IPlugin
    {

        public static int Count = 0;
        public static int Pulses = 1000;

        private ConfigWindow _window = new ConfigWindow();

        #region IPlugin Members

        /// <summary>
        /// Show our config window and add handlers to buttons
        /// </summary>
        public void OnConfig()
        {
            if (!_window.IsVisible)
            {
                _window = new ConfigWindow();
                _window.Show();
            }

        }

        public void OnEnabled()
        {
            //Logging.Write("Enabled Test Plugin");
        }

        public void OnDisabled()
        {
            //Logging.Write("Disabled Test Plugin");
        }

        public void OnLoad()
        {
            //Logging.Write("Loaded Test Plugin");
        }

        public void OnPulse()
        {
            if (Count == 0)
            {
                //Logging.Write("Writing this message every " + Pulses + " pulses");
              
            }

            Count++;


            if (Count >= Pulses)
            {
                Count = 0;
            }
        }

        public void OnShutdown()
        {
            //Logging.Write("Shutdown Test Plugin");
        }

        public string Name
        {
            get { return "TestPlugin"; }
        }

        public Version Version
        {
            get { return new Version(1, 0, 0, 1); }
        }

        public string Author
        {
            get { return "~Unknown~"; }
        }

        public string Description
        {
            get { return "Example test plugin that does almost nothing"; }
        }

        #endregion
    }
}