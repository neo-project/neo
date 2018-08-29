using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neo.Plugins
{
    public abstract class Plugin
    {
        internal static readonly List<ILogPlugin> Loggers = new List<ILogPlugin>();
        internal static readonly List<IPolicyPlugin> Policies = new List<IPolicyPlugin>();
        internal static readonly List<ILoadingPlugin> LoadingPlugins = new List<ILoadingPlugin>();
        internal static readonly List<IRpcPlugin> RpcPlugins = new List<IRpcPlugin>();

        protected static NeoSystem System { get; private set; }
        public virtual string Name => GetType().Name;
        public virtual Version Version => GetType().Assembly.GetName().Version;

        protected Plugin()
        {
            if (this is ILogPlugin logger) Loggers.Add(logger);
            if (this is IPolicyPlugin policy) Policies.Add(policy);
            if (this is ILoadingPlugin loading) LoadingPlugins.Add(loading);
            if (this is IRpcPlugin rpc) RpcPlugins.Add(rpc);
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
    }
}
