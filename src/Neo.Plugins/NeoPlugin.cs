// Copyright (C) 2015-2025 The Neo Project.
//
// NeoPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Neo.Plugins
{
    public abstract class NeoPlugin : IDisposable, IAsyncDisposable
    {
        internal const string PluginConfigurationString = "PluginConfiguration";

        /// <summary>
        /// A list of all loaded plugins.
        /// </summary>
        public static readonly List<NeoPlugin> LoadedPlugins = [];

        /// <summary>
        /// The directory containing the plugin folders. Files can be contained in any subdirectory.
        /// </summary>
        public static readonly string PluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");

        /// <summary>
        /// Indicates the root path of the plugin.
        /// </summary>
        public string RootPath => Path.Combine(PluginDirectory, $"{GetType().Assembly.GetName().Name}");

        /// <summary>
        /// Indicates the location of the plugin configuration file.
        /// </summary>
        public virtual string ConfigFile => Path.Combine(RootPath, "config.json");

        /// <summary>
        /// Indicates the name of the plugin.
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Indicates the description of the plugin.
        /// </summary>
        public virtual string Description => string.Empty;

        /// <summary>
        /// Indicates the version of the plugin.
        /// </summary>
        public virtual Version Version => GetType().Assembly.GetName().Version ?? new Version();

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        protected NeoPlugin()
        {
            LoadedPlugins.Add(this);
            Configure();
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public virtual ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Called when the plugin is loaded and need to load the configure file,
        /// or the configuration file has been modified and needs to be reconfigured.
        /// </summary>
        protected virtual void Configure() { }

        /// <summary>
        /// Loads the configuration file from the path of <see cref="ConfigFile"/>.
        /// </summary>
        /// <returns>The content of the configuration file read.</returns>
        protected IConfigurationSection GetConfiguration() =>
            new ConfigurationBuilder()
                .AddJsonFile(ConfigFile, optional: true)
                .Build()
                .GetSection(PluginConfigurationString);

        /// <summary>
        /// Called when a <see cref="NeoSystem"/> is loaded.
        /// </summary>
        /// <param name="system">The loaded <see cref="NeoSystem"/>.</param>
        protected internal virtual void OnSystemLoaded(NeoSystem system) { }

        internal static void LoadPlugins()
        {
            if (Directory.Exists(PluginDirectory) == false)
                return;

            foreach (var pluginPath in Directory.GetDirectories(PluginDirectory))
            {
                var pluginName = Path.GetFileName(pluginPath);
                var pluginFileName = Path.Combine(PluginDirectory, $"{pluginName}", $"{pluginName}.dll");

                if (File.Exists(pluginFileName) == false)
                    continue;

                // Provides isolated, dynamic loading and unloading of assemblies and
                // their dependencies. Each ALC instance manages the resolution and
                // loading of assemblies and supports loading multiple versions of the
                // same assembly within a process by isolating them in different contexts.
                var assemblyName = new AssemblyName(pluginName);
                var pluginAssemblyContext = new PluginAssemblyLoadContext(assemblyName);
                var pluginAssembly = pluginAssemblyContext.LoadFromAssemblyName(assemblyName);

                var neoPluginClassType = pluginAssembly.ExportedTypes
                    .FirstOrDefault(
                        static f =>
                            f.IsAssignableTo(typeof(NeoPlugin)) && f.IsAbstract == false
                    );

                if (neoPluginClassType is null)
                    pluginAssemblyContext.Unload();
                else
                {
                    var pluginClassConstructor = neoPluginClassType.GetConstructor(Type.EmptyTypes);

                    if (pluginClassConstructor is null)
                        pluginAssemblyContext.Unload();
                    else
                    {
                        try
                        {
                            pluginClassConstructor.Invoke(null);
                        }
                        catch (Exception)
                        {
                            pluginAssemblyContext.Unload();
                        }
                    }
                }
            }
        }
    }
}
