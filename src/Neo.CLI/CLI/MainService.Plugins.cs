// Copyright (C) 2015-2024 The Neo Project.
//
// MainService.Plugins.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Util.Internal;
using Microsoft.Extensions.Configuration;
using Neo.ConsoleService;
using Neo.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "install" command
        /// </summary>
        /// <param name="pluginName">Plugin name</param>
        [ConsoleCommand("install", Category = "Plugin Commands")]
        private void OnInstallCommand(string pluginName)
        {
            if (PluginExists(pluginName))
            {
                ConsoleHelper.Warning($"Plugin already exist.");
                return;
            }

            var result = InstallPluginAsync(pluginName).GetAwaiter().GetResult();
            if (result)
            {
                var asmName = Assembly.GetExecutingAssembly().GetName().Name;
                ConsoleHelper.Info("", $"Install successful, please restart \"{asmName}\".");
            }
        }

        /// <summary>
        /// Force to install a plugin again. This will overwrite
        /// existing plugin files, in case of any file missing or
        /// damage to the old version.
        /// </summary>
        /// <param name="pluginName">name of the plugin</param>
        [ConsoleCommand("reinstall", Category = "Plugin Commands", Description = "Overwrite existing plugin by force.")]
        private void OnReinstallCommand(string pluginName)
        {
            var result = InstallPluginAsync(pluginName, overWrite: true).GetAwaiter().GetResult();
            if (result)
            {
                var asmName = Assembly.GetExecutingAssembly().GetName().Name;
                ConsoleHelper.Info("", $"Reinstall successful, please restart \"{asmName}\".");
            }
        }

        /// <summary>
        /// Download plugin from github release
        /// The function of download and install are divided
        /// for the consideration of `update` command that
        /// might be added in the future.
        /// </summary>
        /// <param name="pluginName">name of the plugin</param>
        /// <param name="pluginVersion"></param>
        /// <param name="prerelease"></param>
        /// <returns>Downloaded content</returns>
        private static async Task<Stream> DownloadPluginAsync(string pluginName, Version pluginVersion, bool prerelease = false)
        {
            using var httpClient = new HttpClient();

            var asmName = Assembly.GetExecutingAssembly().GetName();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new(asmName.Name!, asmName.Version!.ToString(3)));

            var json = await httpClient.GetFromJsonAsync<JsonArray>(Settings.Default.Plugins.DownloadUrl)
                ?? throw new HttpRequestException($"Failed: {Settings.Default.Plugins.DownloadUrl}");

            var pluginVersionString = $"v{pluginVersion.ToString(3)}";

            var jsonRelease = json.AsArray()
                .FirstOrDefault(s =>
                    s?["tag_name"]?.GetValue<string>() == pluginVersionString &&
                    s["prerelease"]?.GetValue<bool>() == prerelease);

            if (jsonRelease == null)
            {
                jsonRelease = json.AsArray()
                    .Where(s => s?["prerelease"]?.GetValue<bool>() == prerelease)
                    .Select(s =>
                    {
                        var tagName = s["tag_name"]?.GetValue<string>();
                        return Version.TryParse(tagName?[1..], out var version)
                            ? new { JsonObject = s, Version = version }
                            : null;
                    })
                    .OfType<dynamic>()
                    .OrderByDescending(s => s.Version)
                    .Select(s => s.JsonObject)
                    .FirstOrDefault();

                if (jsonRelease != null)
                {

                    var latestVersion = Version.Parse(jsonRelease["tag_name"]!.GetValue<string>()[1..]);
                    if (latestVersion < pluginVersion)
                    {
                        var latestDownloadUrl = $"https://github.com/neo-project/neo-modules/releases/download/v{latestVersion}/{pluginName}.zip";
                        ConsoleHelper.Info($"Could not find the corresponding version, installing the latest: v{latestVersion}");
                        return await httpClient.GetStreamAsync(latestDownloadUrl);
                    }
                }

                throw new Exception($"Could not find Release {pluginVersion}");
            }

            var jsonAssets = jsonRelease["assets"]?.AsArray()
                ?? throw new Exception("Could not find any Plugins");

            var jsonPlugin = jsonAssets
                .FirstOrDefault(s =>
                    Path.GetFileNameWithoutExtension(s?["name"]?.GetValue<string>() ?? string.Empty)
                        .Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                ?? throw new Exception($"Could not find {pluginName}");

            var downloadUrl = jsonPlugin["browser_download_url"]?.GetValue<string>()
                ?? throw new Exception("Could not find download URL");

            return await httpClient.GetStreamAsync(downloadUrl);
        }

        /// <summary>
        /// Install plugin from stream
        /// </summary>
        /// <param name="pluginName">Name of the plugin</param>
        /// <param name="installed">Dependency set</param>
        /// <param name="overWrite">Install by force for `update`</param>
        public async Task<bool> InstallPluginAsync(
            string pluginName,
            HashSet<string>? installed = null,
            bool overWrite = false)
        {
            installed ??= new HashSet<string>();
            if (!installed.Add(pluginName)) return false;
            if (!overWrite && PluginExists(pluginName)) return false;

            try
            {

                using var stream = await DownloadPluginAsync(pluginName, Settings.Default.Plugins.Version, Settings.Default.Plugins.Prerelease);

                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                var entry = zip.Entries.FirstOrDefault(p => p.Name == "config.json");
                if (entry is not null)
                {
                    await using var es = entry.Open();
                    await InstallDependenciesAsync(es, installed);
                }
                zip.ExtractToDirectory("./", true);
                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error(ex?.InnerException?.Message ?? ex!.Message);
            }
            return false;
        }

        /// <summary>
        /// Install the dependency of the plugin
        /// </summary>
        /// <param name="config">plugin config path in temp</param>
        /// <param name="installed">Dependency set</param>
        private async Task InstallDependenciesAsync(Stream config, HashSet<string> installed)
        {
            var dependency = new ConfigurationBuilder()
                .AddJsonStream(config)
                .Build()
                .GetSection("Dependency");

            if (!dependency.Exists()) return;
            var dependencies = dependency.GetChildren().Select(p => p.Get<string>()).ToArray();
            if (dependencies.Length == 0) return;

            foreach (var plugin in dependencies.Where(p => p is not null && !PluginExists(p)))
            {
                ConsoleHelper.Info($"Installing dependency: {plugin}");
                await InstallPluginAsync(plugin!, installed);
            }
        }

        /// <summary>
        /// Check that the plugin has all necessary files
        /// </summary>
        /// <param name="pluginName"> Name of the plugin</param>
        /// <returns></returns>
        private static bool PluginExists(string pluginName)
        {
            return Plugin.Plugins.Any(p => p.Name.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Process "uninstall" command
        /// </summary>
        /// <param name="pluginName">Plugin name</param>
        [ConsoleCommand("uninstall", Category = "Plugin Commands")]
        private void OnUnInstallCommand(string pluginName)
        {
            if (!PluginExists(pluginName))
            {
                ConsoleHelper.Error("Plugin not found");
                return;
            }

            foreach (var p in Plugin.Plugins)
            {
                try
                {
                    using var reader = File.OpenRead($"Plugins/{p.Name}/config.json");
                    if (new ConfigurationBuilder()
                        .AddJsonStream(reader)
                        .Build()
                        .GetSection("Dependency")
                        .GetChildren()
                        .Select(s => s.Get<string>())
                        .Any(a => a is not null && a.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        ConsoleHelper.Error($"{pluginName} is required by other plugins.");
                        ConsoleHelper.Info("Info: ", $"If plugin is damaged try to reinstall.");
                        return;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            try
            {
                Directory.Delete($"Plugins/{pluginName}", true);
            }
            catch (IOException) { }
            ConsoleHelper.Info("", "Uninstall successful, please restart neo-cli.");
        }

        /// <summary>
        /// Process "plugins" command
        /// </summary>
        [ConsoleCommand("plugins", Category = "Plugin Commands")]
        private void OnPluginsCommand()
        {
            try
            {
                var plugins = GetPluginListAsync().GetAwaiter().GetResult();
                if (plugins == null) return;
                plugins
                .Order()
                .ForEach(f =>
                {
                    var installedPlugin = Plugin.Plugins.SingleOrDefault(pp => string.Equals(pp.Name, f, StringComparison.CurrentCultureIgnoreCase));
                    if (installedPlugin != null)
                    {
                        var maxLength = plugins.Select(s => s.Length).OrderDescending().First();
                        string tabs = string.Empty;
                        if (f.Length < maxLength)
                            tabs = "\t";
                        ConsoleHelper.Info("", $"[Installed]\t {f,6}{tabs}", "  @", $"{installedPlugin.Version.ToString(3)}  {installedPlugin.Description}");
                    }
                    else
                        ConsoleHelper.Info($"[Not Installed]\t {f}");
                });
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error(ex!.InnerException?.Message ?? ex!.Message);
            }
        }

        private async Task<IEnumerable<string>> GetPluginListAsync()
        {
            using var httpClient = new HttpClient();

            var asmName = Assembly.GetExecutingAssembly().GetName();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new(asmName.Name!, asmName.Version!.ToString(3)));

            var json = await httpClient.GetFromJsonAsync<JsonArray>(Settings.Default.Plugins.DownloadUrl) ?? throw new HttpRequestException($"Failed: {Settings.Default.Plugins.DownloadUrl}");
            return json.AsArray()
                .Where(w =>
                    w != null &&
                    w["tag_name"]!.GetValue<string>() == $"v{Settings.Default.Plugins.Version.ToString(3)}")
                .SelectMany(s => s!["assets"]!.AsArray())
                .Select(s => Path.GetFileNameWithoutExtension(s!["name"]!.GetValue<string>()));
        }
    }
}
