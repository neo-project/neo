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
using System.Runtime.Loader;

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
        if (context == AssemblyLoadContext.Default)
        {
            Console.Error.WriteLine("The plugin was loaded into the default context.");
            return 1;
        }

        var hostAssembly = typeof(Plugin).Assembly.GetName().Name;
        if (context.Assemblies.Any(a => a.GetName().Name == hostAssembly))
        {
            Console.Error.WriteLine("The plugin context loaded a second copy of the host assembly.");
            return 1;
        }

        var method = plugin.GetType().GetMethod(nameof(FixturePlugin.GetDependencyAssemblyLocation))!;
        if (method.Invoke(plugin, null) as string != dependencyPath)
        {
            Console.Error.WriteLine("The plugin dependency was loaded from outside its directory.");
            return 1;
        }

        return 0;
    }
}
