using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CoolFishNS.Properties;
using CoolFishNS.Utilities;

namespace CoolFishNS.PluginSystem
{
    internal static class PluginManager
    {
        public static Dictionary<string, PluginContainer> Plugins = new Dictionary<string, PluginContainer>();

        internal static bool ShouldPulse = true;

        internal static Thread PluginThread;

        internal static void StartPlugins()
        {
            ShouldPulse = true;

            PluginThread = new Thread(PluginPulse) {IsBackground = true};

            PluginThread.Start();
        }

        internal static void StopPlugins()
        {
            ShouldPulse = false; // stop pulsing plugins
            if (!PluginThread.Join(5000)) // wait for the plugin thread to end
            {
                Logging.Log(Resources.FailedToStopPlugins);
            }
            PluginThread = null;
        }

        internal static void ShutDownPlugins()
        {
            foreach (PluginContainer value in Plugins.Values)
            {
                try
                {
                    value.Plugin.OnShutdown(); // call all plugin shutdown procedures
                }
                catch (Exception ex)
                {
                    Logging.Write("Exception shutting down plugin: " + value.Plugin.Name);
                    Logging.Log(ex);
                }
            }
        }

        internal static IEnumerable<IPlugin> GetEnabledPlugins()
        {
            return
                Plugins.Where(plugin => plugin.Value != null && plugin.Value.Enabled).Select(
                    container => container.Value.Plugin);
        }

        internal static void PulseAllPlugins()
        {
            foreach (IPlugin enabledPlugin in GetEnabledPlugins())
            {
                try
                {
                    if (ShouldPulse)
                    {
                        enabledPlugin.OnPulse();
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write(Resources.PluginPulseException, enabledPlugin.Name);
                    Logging.Log(ex);
                    Plugins[enabledPlugin.Name].Enabled = false;
                }
            }
        }


        internal static void PluginPulse()
        {
            while (ShouldPulse)
            {
                PulseAllPlugins();

                Thread.Sleep(1000/60); // About 60 FPS pulses
            }
        }

        internal static void LoadPlugins()
        {
            if (!Directory.Exists(Utilities.Utilities.ApplicationPath + "\\Plugins"))
            {
                Directory.CreateDirectory(Utilities.Utilities.ApplicationPath + "\\Plugins");
            }
            Plugins.Clear();
            string[] plugins = Directory.GetFiles(Utilities.Utilities.ApplicationPath + "\\Plugins", "*.dll");

            foreach (string plugin in plugins)
            {
                try
                {
                    Assembly asm = Assembly.LoadFrom(plugin);
                    foreach (Type bin in asm.GetTypes())
                    {
                        if (bin.IsClass && !bin.IsAbstract && typeof (IPlugin).IsAssignableFrom(bin))
                        {
                            var instance = Activator.CreateInstance(bin) as IPlugin;
                            if (instance != null)
                            {
                                instance.OnLoad();
                                Plugins[instance.Name] = new PluginContainer(instance);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write("Failed to load Plugin: " + plugin);
                    Logging.Log(ex);
                }
            }
        }
    }
}