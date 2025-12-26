// Copyright (C) 2015-2025 The Neo Project.
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
using System.Runtime.Loader;

namespace Neo.UnitTests.Plugins;

[TestClass]
public class UT_PluginLoadContext
{
    private const string FixtureAssemblyName = "Neo.PluginFixture";
    private const string DependencyAssemblyName = "Neo.PluginFixture.Dependency";
    private static readonly Lock s_locker = new();

    [TestMethod]
    public void TestLoadPluginsUsesIsolatedContext()
    {
        lock (s_locker)
        {
            try
            {
                var pluginRoot = PrepareFixturePluginDirectory();
                Plugin.Plugins.Clear();

                Plugin.LoadPlugins();

                var plugin = Plugin.Plugins.SingleOrDefault(p => p.GetType().Assembly.GetName().Name == FixtureAssemblyName);
                Assert.IsNotNull(plugin);

                var loadContext = AssemblyLoadContext.GetLoadContext(plugin.GetType().Assembly);
                Assert.IsNotNull(loadContext);
                Assert.AreNotSame(AssemblyLoadContext.Default, loadContext);
                Assert.AreEqual(Path.Combine(pluginRoot, $"{FixtureAssemblyName}.dll"), plugin.GetType().Assembly.Location);
            }
            finally
            {
                Plugin.Plugins.Clear();
            }
        }
    }

    [TestMethod]
    public void TestLoadPluginsResolvesDependencyFromPluginDirectory()
    {
        lock (s_locker)
        {
            try
            {
                var pluginRoot = PrepareFixturePluginDirectory();
                Plugin.Plugins.Clear();

                Plugin.LoadPlugins();

                var plugin = Plugin.Plugins.SingleOrDefault(p => p.GetType().Assembly.GetName().Name == FixtureAssemblyName);
                Assert.IsNotNull(plugin);

                var method = plugin.GetType().GetMethod("GetDependencyAssemblyLocation");
                Assert.IsNotNull(method);

                var location = method.Invoke(plugin, null) as string;
                Assert.IsFalse(string.IsNullOrEmpty(location));
                Assert.AreEqual(Path.Combine(pluginRoot, $"{DependencyAssemblyName}.dll"), location);
            }
            finally
            {
                Plugin.Plugins.Clear();
            }
        }
    }

    private static string PrepareFixturePluginDirectory()
    {
        var pluginsDir = Plugin.PluginsDirectory;
        Directory.CreateDirectory(pluginsDir);
        var pluginRoot = Path.Combine(pluginsDir, FixtureAssemblyName);
        Directory.CreateDirectory(pluginRoot);

        var sourcePluginPath = Path.Combine(AppContext.BaseDirectory, $"{FixtureAssemblyName}.dll");
        var sourceDependencyPath = Path.Combine(AppContext.BaseDirectory, $"{DependencyAssemblyName}.dll");

        Assert.IsTrue(File.Exists(sourcePluginPath), $"Missing fixture plugin assembly: {sourcePluginPath}");
        Assert.IsTrue(File.Exists(sourceDependencyPath), $"Missing fixture dependency assembly: {sourceDependencyPath}");

        File.Copy(sourcePluginPath, Path.Combine(pluginRoot, $"{FixtureAssemblyName}.dll"), true);
        File.Copy(sourceDependencyPath, Path.Combine(pluginRoot, $"{DependencyAssemblyName}.dll"), true);

        return pluginRoot;
    }
}
