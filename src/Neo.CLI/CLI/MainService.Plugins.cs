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
using Neo.Cryptography;
using Neo.Json;
using Neo.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private async Task OnInstallCommandAsync(string pluginName)
        {
            if (PluginExists(pluginName))
            {
                ConsoleHelper.Warning($"Plugin already exist.");
                return;
            }

            await InstallPluginAsync(pluginName);
            ConsoleHelper.Warning("Install successful, please restart neo-cli.");
        }

        /// <summary>
        /// Force to install a plugin again. This will overwrite
        /// existing plugin files, in case of any file missing or
        /// damage to the old version.
        /// </summary>
        /// <param name="pluginName">name of the plugin</param>
        [ConsoleCommand("reinstall", Category = "Plugin Commands", Description = "Overwrite existing plugin by force.")]
        private async Task OnReinstallCommand(string pluginName)
        {
            await InstallPluginAsync(pluginName, overWrite: true);
            ConsoleHelper.Warning("Reinstall successful, please restart neo-cli.");
        }

        /// <summary>
        /// Download plugin from github release
        /// The function of download and install are divided
        /// for the consideration of `update` command that
        /// might be added in the future.
        /// </summary>
        /// <param name="pluginName">name of the plugin</param>
        /// <returns>Downloaded content</returns>
        private async Task<MemoryStream> DownloadPluginAsync(string pluginName)
        {
            var url =
                $"https://github.com/neo-project/neo-modules/releases/download/v{typeof(Plugin).Assembly.GetVersion()}/{pluginName}.zip";
            using HttpClient http = new();
            var response = await http.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                response.Dispose();
                var versionCore = typeof(Plugin).Assembly.GetName().Version!;
                HttpRequestMessage request = new(HttpMethod.Get,
                    "https://api.github.com/repos/neo-project/neo-modules/releases");
                request.Headers.UserAgent.ParseAdd(
                    $"{GetType().Assembly.GetName().Name}/{GetType().Assembly.GetVersion()}");
                using var responseApi = await http.SendAsync(request);
                var buffer = await responseApi.Content.ReadAsByteArrayAsync();
                if (JToken.Parse(buffer) is not JArray arr)
                    throw new Exception("Plugin doesn't exist.");

                var asset = (arr
                    .Where(p => p?["tag_name"] is not null && p?["assets"] is not null)
                    .Where(p => !p!["tag_name"]!.GetString().Contains('-'))
                    .Select(p => new
                    {
                        Version = Version.Parse(p!["tag_name"]!.GetString().TrimStart('v')),
                        Assets = p["assets"] as JArray
                    })
                    .OrderByDescending(p => p.Version)
                    .First(p => p.Version <= versionCore).Assets?
                    .FirstOrDefault(p => p?["name"]?.GetString() == $"{pluginName}.zip"))
                    ?? throw new Exception("Plugin doesn't exist.");
                response = await http.GetAsync(asset["browser_download_url"]?.GetString());
            }

            using (response)
            {
                var totalRead = 0L;
                var buffer = new byte[1024];
                int read;
                await using var stream = await response.Content.ReadAsStreamAsync();
                ConsoleHelper.Info("From ", url);
                var output = new MemoryStream();
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    output.Write(buffer, 0, read);
                    totalRead += read;
                    Console.Write(
                        $"\rDownloading {pluginName}.zip {totalRead / 1024}KB/{response.Content.Headers.ContentLength / 1024}KB {totalRead * 100 / response.Content.Headers.ContentLength}%");
                }

                Console.WriteLine();
                return output;
            }
        }

        /// <summary>
        /// Install plugin from stream
        /// </summary>
        /// <param name="pluginName">Name of the plugin</param>
        /// <param name="installed">Dependency set</param>
        /// <param name="overWrite">Install by force for `update`</param>
        private async Task InstallPluginAsync(string pluginName, HashSet<string>? installed = null,
            bool overWrite = false)
        {
            installed ??= new HashSet<string>();
            if (!installed.Add(pluginName)) return;
            if (!overWrite && PluginExists(pluginName)) return;

            await using var stream = await DownloadPluginAsync(pluginName);
            ConsoleHelper.Info("SHA256: ", $"{stream.ToArray().Sha256().ToHexString()}");

            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = zip.Entries.FirstOrDefault(p => p.Name == "config.json");
            if (entry is not null)
            {
                await using var es = entry.Open();
                await InstallDependenciesAsync(es, installed);
            }
            zip.ExtractToDirectory("./", true);
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
                ConsoleHelper.Warning("Plugin not found");
                return;
            }

            foreach (var p in Plugin.Plugins)
            {
                try
                {
                    using var reader = File.OpenRead($"./Plugins/{p.Name}/config.json");
                    if (new ConfigurationBuilder()
                        .AddJsonStream(reader)
                        .Build()
                        .GetSection("Dependency")
                        .GetChildren()
                        .Select(d => d.Get<string>())
                        .Any(v => v is not null && v.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        ConsoleHelper.Error(
                            $"Can not uninstall. Other plugins depend on this plugin, try `reinstall {pluginName}` if the plugin is broken.");
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
            ConsoleHelper.Info("Uninstall successful, please restart neo-cli.");
        }

        /// <summary>
        /// Process "plugins" command
        /// </summary>
        [ConsoleCommand("plugins", Category = "Plugin Commands")]
        private async void OnPluginsCommandAsync()
        {
            var plugins = await GetPluginListAsync();
            var installed = Plugin.Plugins.Select(p => p.Name.ToLowerInvariant());
            plugins.ForEach(
                p =>
                {
                    if (p.Contains(".zip")) p = p.Substring(0, p.Length - 4);
                    var installedPlugin = Plugin.Plugins.Where(pp => string.Equals(pp.Name, p, StringComparison.CurrentCultureIgnoreCase)).ToArray();
                    if (installedPlugin.Length == 1)
                    {
                        var plugin = $"(installed) {p}";
                        plugin = plugin.PadLeft(25);
                        Console.WriteLine($"\t{plugin,-25} @{installedPlugin[0].Version} {installedPlugin[0].Description}");
                    }
                    else
                    {
                        var plugin = $"{p}";
                        plugin = plugin.PadLeft(25);
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"\t{plugin,-25}");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                });
        }

        private async Task<IEnumerable<string>?> GetPluginListAsync()
        {
            using HttpClient http = new();

            var versionCore = typeof(Plugin).Assembly.GetName().Version!;
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/repos/neo-project/neo-modules/releases");
            request.Headers.UserAgent.ParseAdd(
                $"{GetType().Assembly.GetName().Name}/{GetType().Assembly.GetVersion()}");
            using var responseApi = await http.SendAsync(request);
            var buffer = await responseApi.Content.ReadAsByteArrayAsync();
            if (JToken.Parse(buffer) is not JArray arr)
                throw new Exception("Plugin doesn't exist.");
            return arr
                .Where(p => p?["tag_name"] is not null && p["assets"] is not null)
                .Where(p => !p!["tag_name"]!.GetString().Contains('-'))
                .Select(p => new
                {
                    Version = Version.Parse(p!["tag_name"]!.GetString().TrimStart('v')),
                    Assets = p["assets"] as JArray
                })
                .OrderByDescending(p => p.Version)
                .First(p => p.Version <= versionCore).Assets?
                .Where(p => p?["name"]?.GetString() is not null)
                .Select(p => p!["name"]!.GetString());
        }
    }
}
