using Microsoft.Extensions.Configuration;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neo.Plugins
{
    public abstract class Plugin
    {
        public static readonly List<Plugin> Plugins = new List<Plugin>();
        private static readonly List<ILogPlugin> Loggers = new List<ILogPlugin>();
        internal static readonly List<IPolicyPlugin> Policies = new List<IPolicyPlugin>();
        internal static readonly List<IRpcPlugin> RpcPlugins = new List<IRpcPlugin>();
        internal static readonly List<IPersistencePlugin> PersistencePlugins = new List<IPersistencePlugin>();

        private static readonly string pluginsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
        private static readonly FileSystemWatcher configWatcher;

        protected static NeoSystem System { get; private set; }
        public virtual string Name => GetType().Name;
        public virtual Version Version => GetType().Assembly.GetName().Version;
        public virtual string ConfigFile => Path.Combine(pluginsPath, GetType().Assembly.GetName().Name, "config.json");

        static Plugin()
        {
            if (Directory.Exists(pluginsPath))
            {
                configWatcher = new FileSystemWatcher(pluginsPath, "*.json")
                {
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
                };
                configWatcher.Changed += ConfigWatcher_Changed;
                configWatcher.Created += ConfigWatcher_Changed;
            }
        }

        protected Plugin()
        {
            Plugins.Add(this);

            if (this is ILogPlugin logger) Loggers.Add(logger);
            if (this is IPolicyPlugin policy) Policies.Add(policy);
            if (this is IRpcPlugin rpc) RpcPlugins.Add(rpc);
            if (this is IPersistencePlugin persistence) PersistencePlugins.Add(persistence);

            Configure();
        }

        public static bool CheckPolicy(Transaction tx)
        {
            foreach (IPolicyPlugin plugin in Policies)
                if (!plugin.FilterForMemoryPool(tx))
                    return false;
            return true;
        }

        public abstract void Configure();

        private static void ConfigWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.ConfigFile == e.FullPath)
                {
                    plugin.Configure();
                    plugin.Log($"Reloaded config for {plugin.Name}");
                    break;
                }
            }
        }

        protected IConfigurationSection GetConfiguration()
        {
            return new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true).Build().GetSection("PluginConfiguration");
        }

        internal static void LoadPlugins(NeoSystem system)
        {
            System = system;
            if (!Directory.Exists(pluginsPath)) return;
            foreach (string filename in Directory.EnumerateFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                Assembly assembly = Assembly.LoadFile(filename);
                foreach (Type type in assembly.ExportedTypes)
                {
                    if (!type.IsSubclassOf(typeof(Plugin))) continue;
                    if (type.IsAbstract) continue;

                    ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                    try
                    {
                        constructor?.Invoke(null);
                    }
                    catch { }
                }
            }
        }

        protected void Log(string message, LogLevel level = LogLevel.Info)
        {
            Log($"{nameof(Plugin)}:{Name}", level, message);
        }

        public static void Log(string source, LogLevel level, string message)
        {
            foreach (ILogPlugin plugin in Loggers)
                plugin.Log(source, level, message);
        }

        protected virtual bool OnMessage(object message) => false;

        public static bool SendMessage(object message)
        {
            foreach (Plugin plugin in Plugins)
                if (plugin.OnMessage(message))
                    return true;
            return false;
        }
    }
}
