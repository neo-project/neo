// Copyright (C) 2015-2026 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;

namespace Neo.PluginFixture;

internal static class Program
{
    public static int Main(string[] args)
    {
        var fixtureRoot = args[0];
        var assemblyPath = args[1];
        var dependencyPath = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "Neo.PluginFixture.Dependency.dll");
        AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", fixtureRoot + Path.DirectorySeparatorChar);
        var loader = typeof(Plugin).GetMethod("LoadPlugins", BindingFlags.NonPublic | BindingFlags.Static)!;
        loader.Invoke(null, null);

        var plugin = Plugin.Plugins.Single(p => p.GetType().Assembly.Location == assemblyPath);
        var assembly = plugin.GetType().Assembly;
        var context = AssemblyLoadContext.GetLoadContext(assembly)!;
        var hostAssembly = typeof(Plugin).Assembly.GetName().Name;
        var method = plugin.GetType().GetMethod(nameof(FixturePlugin.GetDependencyAssemblyLocation))!;
        var dependencyLocation = (string)method.Invoke(plugin, null)!;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            loadedInDefaultContext = context == AssemblyLoadContext.Default,
            ownsHostAssembly = context.Assemblies.Any(a => a.GetName().Name == hostAssembly),
            usesLocalDependency = dependencyLocation == dependencyPath,
            nativeAssetLoaded = CanLoadNativeImage("NeoPluginFixtureNative", context),
            nativeAssetLoadedByExactName = CanLoadNativeImage("NeoPluginFixtureNativeExact.bin", context),
            missingNativeAssetLoaded = CanLoadNativeImage("NeoPluginFixtureMissingNative", context)
        }));

        return 0;
    }

    private static bool CanLoadNativeImage(string libraryName, AssemblyLoadContext context)
    {
        var loadNative = context.GetType().GetMethod("LoadUnmanagedDll", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var handle = (IntPtr)loadNative.Invoke(context, [libraryName])!;
        if (handle == IntPtr.Zero)
            return false;
        try
        {
            return NativeLibrary.TryGetExport(handle, "corehost_main", out _);
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }
}
