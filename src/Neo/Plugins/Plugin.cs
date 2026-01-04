// Copyright (C) 2015-2026 The Neo Project.
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
using System.Reflection;
using System.Runtime.Loader;
using static System.IO.Path;

namespace Neo.Plugins;

/// <summary>
/// Represents the base class of all plugins. Any plugin should inherit this class.
/// The plugins are automatically loaded when the process starts.
/// </summary>
public abstract class Plugin : IDisposable
{
    /// <summary>
    /// A list of all loaded plugins.
    /// </summary>
    public static readonly List<Plugin> Plugins = [];

    /// <summary>
    /// The directory containing the plugin folders. Files can be contained in any subdirectory.
    /// </summary>
    public static readonly string PluginsDirectory =
        Combine(GetDirectoryName(AppContext.BaseDirectory)!, "Plugins");

    private static readonly FileSystemWatcher? s_configWatcher;
    private static readonly List<PluginLoadContext> s_pluginLoadContexts = [];

    /// <summary>
    /// Indicates the root path of the plugin.
    /// </summary>
    public string RootPath => Combine(PluginsDirectory, GetType().Assembly.GetName().Name!);

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
    public virtual string Path => Combine(RootPath, GetType().Assembly.ManifestModule.ScopeName);

    /// <summary>
    /// Indicates the version of the plugin.
    /// </summary>
    public virtual Version Version => GetType().Assembly.GetName().Version ?? new Version();

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
        if (!Directory.Exists(PluginsDirectory)) return;
        s_configWatcher = new FileSystemWatcher(PluginsDirectory)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime |
                           NotifyFilters.LastWrite | NotifyFilters.Size,
        };
        s_configWatcher.Changed += ConfigWatcher_Changed;
        s_configWatcher.Created += ConfigWatcher_Changed;
        s_configWatcher.Renamed += ConfigWatcher_Changed;
        s_configWatcher.Deleted += ConfigWatcher_Changed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    protected Plugin()
    {
        Plugins.Add(this);
        Configure();
    }

    ~Plugin()
    {
        Dispose(false);
    }

    /// <summary>
    /// Called when the plugin is loaded and need to load the configure file,
    /// or the configuration file has been modified and needs to be reconfigured.
    /// </summary>
    protected virtual void Configure() { }

    private static void ConfigWatcher_Changed(object? sender, FileSystemEventArgs e)
    {
        switch (GetExtension(e.Name))
        {
            case ".json":
            case ".dll":
                Utility.Log(nameof(Plugin), LogLevel.Warning,
                    $"File {e.Name} is {e.ChangeType}, please restart node.");
                break;
        }
    }

    private sealed class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly string _pluginDirectory;
        private readonly HashSet<string> _sharedAssemblies;

        public PluginLoadContext(string mainAssemblyPath)
            : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
            _pluginDirectory = GetDirectoryName(mainAssemblyPath)!;
            _sharedAssemblies = new(StringComparer.OrdinalIgnoreCase)
            {
                typeof(Plugin).Assembly.GetName().Name!
            };
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (_sharedAssemblies.Contains(assemblyName.Name!))
                return Default.LoadFromAssemblyName(assemblyName);

            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            if (path != null)
                return LoadFromAssemblyPath(path);

            if (assemblyName.Name is null)
                return null;

            var localPath = Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
            return File.Exists(localPath) ? LoadFromAssemblyPath(localPath) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path != null)
                return LoadUnmanagedDllFromPath(path);

            var localPath = Combine(_pluginDirectory, unmanagedDllName);
            if (File.Exists(localPath))
                return LoadUnmanagedDllFromPath(localPath);

            if (OperatingSystem.IsWindows())
            {
                localPath = Combine(_pluginDirectory, $"{unmanagedDllName}.dll");
                if (File.Exists(localPath))
                    return LoadUnmanagedDllFromPath(localPath);
            }

            if (OperatingSystem.IsLinux())
            {
                localPath = Combine(_pluginDirectory, $"lib{unmanagedDllName}.so");
                if (File.Exists(localPath))
                    return LoadUnmanagedDllFromPath(localPath);
            }

            if (OperatingSystem.IsMacOS())
            {
                localPath = Combine(_pluginDirectory, $"lib{unmanagedDllName}.dylib");
                if (File.Exists(localPath))
                    return LoadUnmanagedDllFromPath(localPath);
            }

            return IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }

    /// <summary>
    /// Loads the configuration file from the path of <see cref="ConfigFile"/>.
    /// </summary>
    /// <returns>The content of the configuration file read.</returns>
    protected IConfigurationSection GetConfiguration()
    {
        return new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true).Build()
            .GetSection("PluginConfiguration");
    }

    private static int LoadPlugin(Assembly assembly)
    {
        Type[] exportedTypes;

        var assemblyName = assembly.GetName().Name;

        try
        {
            exportedTypes = (Type[])assembly.ExportedTypes;
        }
        catch (Exception ex)
        {
            Utility.Log(nameof(Plugin), LogLevel.Error, $"Failed to load plugin assembly {assemblyName}: {ex}");
            throw;
        }

        var loaded = 0;
        foreach (var type in exportedTypes)
        {
            if (!type.IsSubclassOf(typeof(Plugin))) continue;
            if (type.IsAbstract) continue;

            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null) continue;

            try
            {
                constructor.Invoke(null);
                loaded++;
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(Plugin), LogLevel.Error, $"Failed to initialize plugin type {type.FullName} of {assemblyName}: {ex}");
            }
        }

        return loaded;
    }

    internal static void LoadPlugins()
    {
        if (!Directory.Exists(PluginsDirectory)) return;
        foreach (var rootPath in Directory.GetDirectories(PluginsDirectory))
        {
            var pluginName = new DirectoryInfo(rootPath).Name;
            var mainAssemblyPath = Combine(rootPath, $"{pluginName}.dll");
            if (!File.Exists(mainAssemblyPath))
            {
                Utility.Log(nameof(Plugin), LogLevel.Warning, $"Plugin assembly not found: {mainAssemblyPath}");
                continue;
            }

            try
            {
                var loadContext = new PluginLoadContext(mainAssemblyPath);
                s_pluginLoadContexts.Add(loadContext);
                var assembly = loadContext.LoadFromAssemblyPath(mainAssemblyPath);
                var loaded = LoadPlugin(assembly);
                if (loaded > 0)
                    continue;

                foreach (var filename in Directory.EnumerateFiles(rootPath, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    if (string.Equals(filename, mainAssemblyPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        assembly = loadContext.LoadFromAssemblyPath(filename);
                        if (LoadPlugin(assembly) > 0)
                            break;
                    }
                    catch (Exception ex)
                    {
                        Utility.Log(nameof(Plugin), LogLevel.Error, $"Failed to load plugin assembly file {filename}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(Plugin), LogLevel.Error, $"Failed to load plugin assembly file {mainAssemblyPath}: {ex}");
            }
        }
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
    protected internal virtual void OnSystemLoaded(NeoSystem system) { }

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
