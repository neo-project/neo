// Copyright (C) 2015-2026 The Neo Project.
//
// PluginLoadContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Configuration;
using Neo.Json;
using Neo.VM;
using System.Reflection;
using System.Runtime.Loader;
using static System.IO.Path;

namespace Neo.Plugins;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private static readonly HashSet<string> s_sharedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        typeof(Plugin).Assembly.GetName().Name!,
        typeof(JObject).Assembly.GetName().Name!,
        typeof(ExecutionEngine).Assembly.GetName().Name!,
        typeof(IActorRef).Assembly.GetName().Name!,
        typeof(IConfigurationSection).Assembly.GetName().Name!
    };

    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginDirectory;

    public PluginLoadContext(string mainAssemblyPath)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        _pluginDirectory = GetDirectoryName(mainAssemblyPath)!;
    }

    internal Assembly? LoadPluginAssembly(string path)
    {
        var assemblyName = AssemblyName.GetAssemblyName(path);
        return s_sharedAssemblies.Contains(assemblyName.Name!) ? null : LoadFromAssemblyPath(path);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (s_sharedAssemblies.Contains(assemblyName.Name!))
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
