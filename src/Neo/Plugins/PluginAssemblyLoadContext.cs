// Copyright (C) 2015-2025 The Neo Project.
//
// PluginAssemblyLoadContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Neo.Plugins
{
    internal class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginName;

        public PluginAssemblyLoadContext(AssemblyName pluginAssemblyName) : base(isCollectible: true)
        {
            _pluginName = pluginAssemblyName.Name!;
        }

        [return: MaybeNull]
        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Attempt to load the assembly from the specified plugin path
            var assemblyFile = Path.Combine(Plugin.PluginsDirectory, _pluginName, $"{assemblyName.Name}.dll");

            if (File.Exists(assemblyFile))
            {
                return LoadFromAssemblyPath(assemblyFile);
            }

            // Load plugin dependencies
            assemblyFile = Path.Combine(Plugin.PluginsDirectory, $"{assemblyName.Name}", $"{assemblyName.Name}.dll");
            if (File.Exists(assemblyFile))
            {
                return LoadFromAssemblyPath(assemblyFile);
            }

            // If not found in the plugin path, defer to the default load context
            // This allows shared dependencies (like .NET runtime assemblies) to be resolved
            return null;
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var unmanagedDllFilename = GetUnmanagedDllFilename(Path.GetFileNameWithoutExtension(unmanagedDllName));

            // Checks "Plugins\<Plugin Name>" directory
            var unmanagedDllFile = Path.Combine(Plugin.PluginsDirectory, _pluginName, unmanagedDllFilename);
            if (File.Exists(unmanagedDllFile))
            {
                return LoadUnmanagedDllFromPath(unmanagedDllFile);
            }

            // Checks "Plugins\<Plugin Name>\runtimes" directory
            unmanagedDllFile = Path.Combine(
                Plugin.PluginsDirectory,
                _pluginName,
                "runtimes",
                RuntimeInformation.RuntimeIdentifier,
                "native",
                unmanagedDllFilename);
            if (File.Exists(unmanagedDllFile))
            {
                return LoadUnmanagedDllFromPath(unmanagedDllFile);
            }

            // Checks "runtimes" directory
            unmanagedDllFile = Path.Combine(
                AppContext.BaseDirectory,
                "runtimes",
                RuntimeInformation.RuntimeIdentifier,
                "native",
                unmanagedDllFilename);
            if (File.Exists(unmanagedDllFile))
            {
                return LoadUnmanagedDllFromPath(unmanagedDllFile);
            }

            // Checks "base" directory
            unmanagedDllFile = Path.Combine(
                AppContext.BaseDirectory,
                unmanagedDllFilename);
            if (File.Exists(unmanagedDllFile))
            {
                return LoadUnmanagedDllFromPath(unmanagedDllFile);
            }

            return nint.Zero;
        }

        private static string GetUnmanagedDllFilename(string unmanagedDllName)
        {
            var filename = $"{unmanagedDllName}.dll";

            if (OperatingSystem.IsLinux())
                filename = $"{unmanagedDllName}.so";
            else if (OperatingSystem.IsMacOS())
                filename = $"{unmanagedDllName}.dylib";

            return filename;
        }
    }
}
