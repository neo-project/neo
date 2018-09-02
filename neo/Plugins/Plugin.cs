using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neo.Plugins
{
    public abstract class Plugin
    {
        private static readonly List<Plugin> instances = new List<Plugin>();

        public static IEnumerable<Plugin> Instances => instances;
        public abstract string Name { get; }
        public virtual Version Version => GetType().Assembly.GetName().Version;

        protected Plugin()
        {
            instances.Add(this);
        }

        static Plugin()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins");
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

        public static void LoadPlugins()
        {
        }
    }
}
