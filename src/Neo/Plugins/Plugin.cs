// Copyright (C) 2015-2024 The Neo Project.
//
// Plugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using static System.IO.Path;

namespace Neo.Plugins
{
    /// <summary>
    /// Represents the base class of all plugins. Any plugin should inherit this class.
    /// The plugins are automatically loaded when the process starts.
    /// </summary>
    public abstract class Plugin : IDisposable
    {
        /// <summary>
        /// A list of all loaded plugins.
        /// </summary>
        public static readonly List<Plugin> Plugins = new();

        /// <summary>
        /// The directory containing the plugin folders. Files can be contained in any subdirectory.
        /// </summary>
        public static readonly string PluginsConfigurationDirectory = Combine(AppContext.BaseDirectory, "configs");

        private static readonly FileSystemWatcher configWatcher;

        /// <summary>
        /// Indicates the root path of the plugin.
        /// </summary>
        public string RootPath => PluginsConfigurationDirectory;

        /// <summary>
        /// Indicates the location of the plugin configuration file.
        /// </summary>
        public virtual string ConfigFile => Combine(RootPath, "config.json");

        /// <summary>
        /// Indicates the name of the plugin.
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Indicates the description of the plugin.
        /// </summary>
        public virtual string Description => "";

        /// <summary>
        /// Indicates the location of the plugin dll file.
        /// </summary>
        public virtual string Path => AppContext.BaseDirectory;

        /// <summary>
        /// Indicates the version of the plugin.
        /// </summary>
        public virtual Version Version => GetType().Assembly.GetName().Version;

        /// <summary>
        /// If the plugin should be stopped when an exception is thrown.
        /// Default is StopNode.
        /// </summary>
        protected internal virtual UnhandledExceptionPolicy ExceptionPolicy { get; init; } = UnhandledExceptionPolicy.StopNode;

        /// <summary>
        /// The plugin will be stopped if an exception is thrown.
        /// But it also depends on <see cref="UnhandledExceptionPolicy"/>.
        /// </summary>
        internal bool IsStopped { get; set; }

        static Plugin()
        {
            if (!Directory.Exists(PluginsConfigurationDirectory)) return;
            configWatcher = new FileSystemWatcher(PluginsConfigurationDirectory)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime |
                               NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            configWatcher.Changed += ConfigWatcher_Changed;
            configWatcher.Created += ConfigWatcher_Changed;
            configWatcher.Renamed += ConfigWatcher_Changed;
            configWatcher.Deleted += ConfigWatcher_Changed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        protected Plugin()
        {
            Plugins.Add(this);
            Configure();
        }

        /// <summary>
        /// Called when the plugin is loaded and need to load the configure file,
        /// or the configuration file has been modified and needs to be reconfigured.
        /// </summary>
        protected virtual void Configure()
        {
        }

        private static void ConfigWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            switch (GetExtension(e.Name))
            {
                case ".json":
                    Utility.Log(nameof(Plugin), LogLevel.Warning,
                        $"File {e.Name} is {e.ChangeType}, please restart node.");
                    break;
            }
        }

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Loads the configuration file from the path of <see cref="ConfigFile"/>.
        /// </summary>
        /// <returns>The content of the configuration file read.</returns>
        protected IConfigurationSection GetConfiguration()
        {
            return new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true).Build()
                .GetSection("PluginConfiguration");
        }

        /// <summary>
        /// Write a log for the plugin.
        /// </summary>
        /// <param name="message">The message of the log.</param>
        /// <param name="level">The level of the log.</param>
        protected void Log(object message, LogLevel level = LogLevel.Info)
        {
            Utility.Log($"{nameof(Plugin)}:{Name}", level, message);
        }

        /// <summary>
        /// Called when a message to the plugins is received. The message is sent by calling <see cref="SendMessage"/>.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns><see langword="true"/> if the <paramref name="message"/> has been handled; otherwise, <see langword="false"/>.</returns>
        /// <remarks>If a message has been handled by a plugin, the other plugins won't receive it anymore.</remarks>
        protected virtual bool OnMessage(object message)
        {
            return false;
        }

        /// <summary>
        /// Called when a <see cref="NeoSystem"/> is loaded.
        /// </summary>
        /// <param name="system">The loaded <see cref="NeoSystem"/>.</param>
        protected internal virtual void OnSystemLoaded(NeoSystem system)
        {
        }

        /// <summary>
        /// Sends a message to all plugins. It can be handled by <see cref="OnMessage"/>.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns><see langword="true"/> if the <paramref name="message"/> is handled by a plugin; otherwise, <see langword="false"/>.</returns>
        public static bool SendMessage(object message)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.IsStopped)
                {
                    continue;
                }

                bool result;
                try
                {
                    result = plugin.OnMessage(message);
                }
                catch (Exception ex)
                {
                    Utility.Log(nameof(Plugin), LogLevel.Error, ex);

                    switch (plugin.ExceptionPolicy)
                    {
                        case UnhandledExceptionPolicy.StopNode:
                            throw;
                        case UnhandledExceptionPolicy.StopPlugin:
                            plugin.IsStopped = true;
                            break;
                        case UnhandledExceptionPolicy.Ignore:
                            break;
                        default:
                            throw new InvalidCastException($"The exception policy {plugin.ExceptionPolicy} is not valid.");
                    }

                    continue; // Skip to the next plugin if an exception is handled
                }

                if (result)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
