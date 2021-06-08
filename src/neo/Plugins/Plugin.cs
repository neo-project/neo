using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using static System.IO.Path;
namespace Neo.Plugins
{
    /// <summary>
    /// Represents the base class of all plugins. Any plugin should inherit this class. The plugins are automatically loaded when the process starts.
    /// </summary>
    public abstract class Plugin : IDisposable
    {
        /// <summary>
        /// Indicates the name of the plugin.
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Indicates the description of the plugin.
        /// </summary>
        public virtual string Description => "";

        /// <summary>
        /// Indicates the version of the plugin.
        /// </summary>
        public virtual Version Version => GetType().Assembly.GetName().Version;

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Called when the plugin is loaded and need to load the configure file, or the configuration file has been modified and needs to be reconfigured.
        /// </summary>
        public virtual void Configure(IConfigurationSection configuration)
        {
        }

        /// <summary>
        /// Called when a message to the plugins is received. The messnage is sent by calling <see cref="NeoSystem.SendPluginMessage(object)"/>.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns><see langword="true"/> if the <paramref name="message"/> has been handled; otherwise, <see langword="false"/>.</returns>
        /// <remarks>If a message has been handled by a plugin, the other plugins won't receive it anymore.</remarks>
        internal virtual bool OnMessage(object message)
        {
            return false;
        }

        /// <summary>
        /// Called when a <see cref="NeoSystem"/> is loaded.
        /// </summary>
        /// <param name="system">The loaded <see cref="NeoSystem"/>.</param>
        internal protected virtual void OnSystemLoaded(NeoSystem system)
        {
        }

        // /// <summary>
        // /// Loads the configuration file from the path of <see cref="ConfigFile"/>.
        // /// </summary>
        // /// <returns>The content of the configuration file read.</returns>


        // {
        //     return new ConfigurationBuilder()
        //         .AddJsonFile(ConfigFile, optional: true)
        //         .Build().GetSection("PluginConfiguration");
        // }











        // // <summary>
        // // The directory containing the plugin dll files. Files can be contained in any subdirectory.
        // // </summary>
        // private static readonly string PluginsDirectory = Combine(GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");

        // private static readonly FileSystemWatcher configWatcher;

        // /// <summary>
        // /// Indicates the location of the plugin configuration file.
        // /// </summary>
        // public virtual string ConfigFile => Combine(PluginsDirectory, GetType().Assembly.GetName().Name, "config.json");


        // /// <summary>
        // /// Indicates the location of the plugin dll file.
        // /// </summary>
        // public virtual string Path => Combine(PluginsDirectory, GetType().Assembly.ManifestModule.ScopeName);


        // static Plugin()
        // {
        //     if (Directory.Exists(PluginsDirectory))
        //     {
        //         configWatcher = new FileSystemWatcher(PluginsDirectory)
        //         {
        //             EnableRaisingEvents = true,
        //             IncludeSubdirectories = true,
        //             NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
        //         };
        //         configWatcher.Changed += ConfigWatcher_Changed;
        //         configWatcher.Created += ConfigWatcher_Changed;
        //         AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        //     }
        // }


        // private static void ConfigWatcher_Changed(object sender, FileSystemEventArgs e)
        // {
        //     switch (GetExtension(e.Name))
        //     {
        //         case ".json":
        //             try
        //             {
        //                 Plugins.FirstOrDefault(p => p.ConfigFile == e.FullPath)?.Configure();
        //             }
        //             catch (FormatException) { }
        //             break;
        //         case ".dll":
        //             if (e.ChangeType != WatcherChangeTypes.Created) return;
        //             if (GetDirectoryName(e.FullPath) != PluginsDirectory) return;
        //             try
        //             {
        //                 LoadPlugin(Assembly.Load(File.ReadAllBytes(e.FullPath)));
        //             }
        //             catch { }
        //             break;
        //     }
        // }

        // private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        // {
        //     if (args.Name.Contains(".resources"))
        //         return null;

        //     AssemblyName an = new(args.Name);

        //     Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
        //     if (assembly is null)
        //         assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == an.Name);
        //     if (assembly != null) return assembly;

        //     string filename = an.Name + ".dll";
        //     string path = filename;
        //     if (!File.Exists(path)) path = Combine(GetDirectoryName(Assembly.GetEntryAssembly().Location), filename);
        //     if (!File.Exists(path)) path = Combine(PluginsDirectory, filename);
        //     if (!File.Exists(path)) path = Combine(PluginsDirectory, args.RequestingAssembly.GetName().Name, filename);
        //     if (!File.Exists(path)) return null;

        //     try
        //     {
        //         return Assembly.Load(File.ReadAllBytes(path));
        //     }
        //     catch (Exception ex)
        //     {
        //         Utility.Log(nameof(Plugin), LogLevel.Error, ex);
        //         return null;
        //     }
        // }


        // private static void LoadPlugin(Assembly assembly)
        // {
        //     foreach (Type type in assembly.ExportedTypes)
        //     {
        //         if (!type.IsSubclassOf(typeof(Plugin))) continue;
        //         if (type.IsAbstract) continue;

        //         ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        //         try
        //         {
        //             constructor?.Invoke(null);
        //         }
        //         catch (Exception ex)
        //         {
        //             Utility.Log(nameof(Plugin), LogLevel.Error, ex);
        //         }
        //     }
        // }

 
    }
}
