using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CoolFishNS.Properties;
using CoolFishNS.Utilities;
using NLog;

namespace CoolFishNS.PluginSystem
{
    internal static class PluginManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static Dictionary<string, PluginContainer> Plugins = new Dictionary<string, PluginContainer>();
        private static bool _isRunning;

        internal static void StartPlugins()
        {
            _isRunning = true;
            Task.Run(() => Run());
        }

        internal static void StopPlugins()
        {
            _isRunning = false;
        }

        internal static void ShutDownPlugins()
        {
            foreach (var value in Plugins.Values)
            {
                try
                {
                    value.Plugin.OnShutdown(); // call all plugin shutdown procedures
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception shutting down plugin: " + value.Plugin.Name, ex);
                }
            }
        }

        internal static void Run()
        {
            while (_isRunning)
            {
                PulseAllPlugins();
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
            foreach (var enabledPlugin in GetEnabledPlugins())
            {
                try
                {
                    enabledPlugin.OnPulse();
                }
                catch (Exception ex)
                {
                    Logger.Warn(string.Format(Resources.PluginPulseException, enabledPlugin.Name), ex);
                    Plugins[enabledPlugin.Name].Enabled = false;
                }
            }
        }

        internal static void LoadPlugins()
        {
            if (!Directory.Exists(Constants.ApplicationPath.Value + "\\Plugins"))
            {
                Directory.CreateDirectory(Constants.ApplicationPath.Value + "\\Plugins");
            }
            Plugins.Clear();
            var plugins = Directory.GetFiles(Constants.ApplicationPath.Value + "\\Plugins", "*.dll");

            foreach (var plugin in plugins)
            {
                try
                {
                    var asm = Assembly.LoadFrom(plugin);
                    foreach (var bin in asm.GetTypes())
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
                    Logger.Warn("Failed to load Plugin: " + plugin, ex);
                }
            }
        }
    }
}