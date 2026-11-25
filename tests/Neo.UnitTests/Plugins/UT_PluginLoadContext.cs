// Copyright (C) 2015-2026 The Neo Project.
//
// UT_PluginLoadContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Neo.UnitTests.Plugins;

[TestClass]
public class UT_PluginLoadContext
{
    private const string FixtureAssemblyName = "Neo.PluginFixture";
    private const string DependencyAssemblyName = "Neo.PluginFixture.Dependency";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestLoadPluginDirectoryUsesIsolatedContext(bool nameMatchesDirectory)
    {
        var directory = Directory.CreateTempSubdirectory("neo-plugin-");
        WeakReference? contextReference = null;
        try
        {
            var assemblyName = nameMatchesDirectory ? directory.Name : FixtureAssemblyName;
            var assemblyPath = Path.Combine(directory.FullName, assemblyName + ".dll");
            File.Copy(Path.Combine(AppContext.BaseDirectory, FixtureAssemblyName + ".dll"), assemblyPath);
            var dependencyPath = Path.Combine(directory.FullName, DependencyAssemblyName + ".dll");
            File.Copy(Path.Combine(AppContext.BaseDirectory, DependencyAssemblyName + ".dll"), dependencyPath);

            var hostAssemblyPath = typeof(Plugin).Assembly.Location;
            File.Copy(hostAssemblyPath, Path.Combine(directory.FullName, Path.GetFileName(hostAssemblyPath)));

            VerifyPlugin(directory.FullName, assemblyPath, dependencyPath, out contextReference);
        }
        finally
        {
            // The runtime releases assembly files when the collectible context is reclaimed.
            for (var i = 0; i < 10 && contextReference?.IsAlive == true; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            directory.Delete(recursive: true);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VerifyPlugin(string directory, string assemblyPath, string dependencyPath, out WeakReference contextReference)
    {
        var context = Plugin.LoadPluginDirectory(directory, isCollectible: true);
        Assert.IsNotNull(context);
        contextReference = new WeakReference(context);

        try
        {
            var plugin = Plugin.Plugins.Single(p => AssemblyLoadContext.GetLoadContext(p.GetType().Assembly) == context);
            Assert.AreNotSame(AssemblyLoadContext.Default, context);
            Assert.IsFalse(context.Assemblies.Any(a => a.GetName().Name == typeof(Plugin).Assembly.GetName().Name));
            Assert.AreEqual(assemblyPath, plugin.GetType().Assembly.Location);
            var method = plugin.GetType().GetMethod("GetDependencyAssemblyLocation");
            Assert.IsNotNull(method);
            Assert.AreEqual(dependencyPath, method.Invoke(plugin, null) as string);
        }
        finally
        {
            Plugin.Plugins.RemoveAll(p => AssemblyLoadContext.GetLoadContext(p.GetType().Assembly) == context);
            context.Unload();
        }
    }
}
