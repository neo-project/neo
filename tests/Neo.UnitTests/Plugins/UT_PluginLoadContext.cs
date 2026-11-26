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
using System.Diagnostics;

namespace Neo.UnitTests.Plugins;

[TestClass]
public class UT_PluginLoadContext
{
    private const string FixtureAssemblyName = "Neo.PluginFixture";
    private const string DependencyAssemblyName = "Neo.PluginFixture.Dependency";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestLoadPluginsUsesIsolatedContext(bool nameMatchesDirectory)
    {
        var root = Directory.CreateTempSubdirectory("neo-plugin-");
        var testOutput = AppContext.BaseDirectory;
        var fixture = Path.Combine(testOutput, FixtureAssemblyName + ".dll");
        try
        {
            var pluginsDirectory = root.CreateSubdirectory("Plugins");
            pluginsDirectory.CreateSubdirectory("Empty");
            var directory = pluginsDirectory.CreateSubdirectory("InstalledPlugin");
            var assemblyName = nameMatchesDirectory ? directory.Name : FixtureAssemblyName;
            if (nameMatchesDirectory)
            {
                File.Copy(Path.Combine(testOutput, FixtureAssemblyName + ".deps.json"),
                    Path.Combine(directory.FullName, assemblyName + ".deps.json"));
                File.WriteAllText(Path.Combine(directory.FullName, "Invalid.dll"), "This file is not an assembly.");
            }
            var assemblyPath = Path.Combine(directory.FullName, assemblyName + ".dll");
            File.Copy(fixture, assemblyPath);
            var dependencyFile = DependencyAssemblyName + ".dll";
            File.Copy(Path.Combine(testOutput, dependencyFile), Path.Combine(directory.FullName, dependencyFile));
            var hostAssemblyPath = typeof(Plugin).Assembly.Location;
            File.Copy(hostAssemblyPath, Path.Combine(directory.FullName, Path.GetFileName(hostAssemblyPath)));

            // Windows keeps loaded assembly files open until the child process exits.
            var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
            {
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (var argument in new[]
            {
                "exec", "--runtimeconfig", Path.Combine(testOutput, "Neo.UnitTests.runtimeconfig.json"),
                "--depsfile", Path.Combine(testOutput, "Neo.UnitTests.deps.json"),
                fixture, root.FullName, assemblyPath
            })
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo)!;
            var errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Assert.AreEqual(0, process.ExitCode, await errors);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
