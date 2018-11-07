using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neo.Plugins
{
    public abstract class Plugin
    {
        private static readonly List<Plugin> Plugins = new List<Plugin>();
        private static readonly List<ILogPlugin> Loggers = new List<ILogPlugin>();
        internal static readonly List<IPolicyPlugin> Policies = new List<IPolicyPlugin>();
        internal static readonly List<IRpcPlugin> RpcPlugins = new List<IRpcPlugin>();
        internal static readonly List<IPersistencePlugin> PersistencePlugins = new List<IPersistencePlugin>();

        protected static NeoSystem System { get; private set; }
        public virtual string Name => GetType().Name;
        public virtual Version Version => GetType().Assembly.GetName().Version;

        protected virtual bool OnMessage(object message) => false;

        protected Plugin()
        {
            Plugins.Add(this);
            if (this is ILogPlugin logger) Loggers.Add(logger);
            if (this is IPolicyPlugin policy) Policies.Add(policy);
            if (this is IRpcPlugin rpc) RpcPlugins.Add(rpc);
            if (this is IPersistencePlugin persistence) PersistencePlugins.Add(persistence);
        }

        public static bool CheckPolicy(Transaction tx)
        {
            foreach (IPolicyPlugin plugin in Policies)
                if (!plugin.FilterForMemoryPool(tx))
                    return false;
            return true;
        }

        internal static void LoadPlugins(NeoSystem system)
        {
            System = system;
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            if (!Directory.Exists(path)) return;
            foreach (string filename in Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly))
            {
                Assembly assembly = Assembly.LoadFile(filename);
                foreach (Type type in assembly.ExportedTypes)
                {
                    if (!type.IsSubclassOf(typeof(Plugin))) continue;
                    if (type.IsAbstract) continue;
                    ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor == null) continue;
                    constructor.Invoke(null);
                }
            }
        }

        public static void Log(string source, LogLevel level, string message)
        {
            foreach (ILogPlugin plugin in Loggers)
                plugin.Log(source, level, message);
        }

        public static bool SendMessage(object message)
        {
            foreach (Plugin plugin in Plugins)
                if (plugin.OnMessage(message))
                    return true;
            return false;
        }
    }
}
