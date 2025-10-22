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
    internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string[] _searchPluginPaths;

        public PluginAssemblyLoadContext(string[] searchPaths) 
            : base(isCollectible: true)
        {
            _searchPluginPaths = searchPaths;
        }

        [return: MaybeNull]
        protected override Assembly Load(AssemblyName assemblyName)
        {
            foreach (var path in _searchPluginPaths)
            {
                var assemblyFile = Path.Combine(path, $"{assemblyName.Name}.dll");

                if (File.Exists(assemblyFile))
                {
                    return LoadFromAssemblyPath(assemblyFile);
                }
            }

            // If not found in the plugin path, defer to the default load context
            // This allows shared dependencies (like .NET runtime assemblies) to be resolved
            return null;
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var unmanagedDllFilename = GetUnmanagedDllFilename(Path.GetFileNameWithoutExtension(unmanagedDllName));

            string unmanagedDllFile;

            foreach (var path in _searchPluginPaths)
            {
                // Checks "Plugins\<Plugin Name>" directory
                unmanagedDllFile = Path.Combine(path, unmanagedDllFilename);
                if (File.Exists(unmanagedDllFile))
                {
                    return LoadUnmanagedDllFromPath(unmanagedDllFile);
                }

                // Checks "Plugins\<Plugin Name>\runtimes" directory
                unmanagedDllFile = Path.Combine(
                    path,
                    "runtimes",
                    RuntimeInformation.RuntimeIdentifier,
                    "native",
                    unmanagedDllFilename);
                if (File.Exists(unmanagedDllFile))
                {
                    return LoadUnmanagedDllFromPath(unmanagedDllFile);
                }
            }

            // Fallback to `neo-cli` base directory.
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

            unmanagedDllFile = Path.Combine(AppContext.BaseDirectory, unmanagedDllFilename);
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
