// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MainService_Plugins.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Neo.CLI.UnitTests
{
    [TestClass]
    public class UT_MainService_Plugins
    {
        /// <summary>
        /// Test the plugin version parsing and selection logic
        /// </summary>
        [TestMethod]
        public void TestVersionParsingLogic_ExactMatch()
        {
            // Arrange
            var pluginVersion = new Version("1.0.0");
            var pluginVersionString = $"v{pluginVersion.ToString(3)}";
            var prerelease = false;

            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""{pluginVersionString}"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": [
                        {{
                            ""name"": ""TestPlugin.zip"",
                            ""browser_download_url"": ""https://example.com/TestPlugin.zip""
                        }}
                    ]
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);

            // Act
            var jsonRelease = json
                .FirstOrDefault(s =>
                    s?["tag_name"]?.AsString() == pluginVersionString &&
                    s?["prerelease"]?.AsBoolean() == prerelease);

            // Assert
            Assert.IsNotNull(jsonRelease);
            Assert.AreEqual(pluginVersionString, jsonRelease["tag_name"]?.AsString());
        }

        [TestMethod]
        public void TestVersionParsingLogic_LatestVersionFallback()
        {
            // Arrange
            var requestedVersion = new Version("1.0.0");
            var requestedVersionString = $"v{requestedVersion.ToString(3)}";
            var prerelease = false;

            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""v2.0.0"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": [
                        {{
                            ""name"": ""TestPlugin.zip"",
                            ""browser_download_url"": ""https://example.com/TestPlugin-v2.zip""
                        }}
                    ]
                }},
                {{
                    ""tag_name"": ""v1.5.0"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": [
                        {{
                            ""name"": ""TestPlugin.zip"",
                            ""browser_download_url"": ""https://example.com/TestPlugin-v1.5.zip""
                        }}
                    ]
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);

            // Act - First try exact match
            var jsonRelease = json
                .FirstOrDefault(s =>
                    s?["tag_name"]?.AsString() == requestedVersionString &&
                    s?["prerelease"]?.AsBoolean() == prerelease);

            // If not found, get latest
            if (jsonRelease == null)
            {
                jsonRelease = json
                    .Where(s => s?["prerelease"]?.AsBoolean() == prerelease)
                    .Select(s =>
                    {
                        var tagName = s?["tag_name"]?.AsString();
                        if (tagName != null && tagName.Length > 1 && tagName.StartsWith('v') &&
                            Version.TryParse(tagName[1..], out var version))
                        {
                            return new { JsonObject = s, Version = version };
                        }
                        return null;
                    })
                    .OfType<dynamic>()
                    .OrderByDescending(s => s.Version)
                    .Select(s => s.JsonObject)
                    .FirstOrDefault();
            }

            // Assert
            Assert.IsNotNull(jsonRelease);
            Assert.AreEqual("v2.0.0", jsonRelease["tag_name"]?.AsString());
        }

        [TestMethod]
        public void TestVersionParsingLogic_InvalidTagNames()
        {
            // Arrange
            var requestedVersion = new Version("1.0.0");
            var requestedVersionString = $"v{requestedVersion.ToString(3)}";
            var prerelease = false;

            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""invalid-tag"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": []
                }},
                {{
                    ""tag_name"": ""v"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": []
                }},
                {{
                    ""tag_name"": ""not-a-version"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": []
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);

            // Act - First try exact match
            var jsonRelease = json
                .FirstOrDefault(s =>
                    s?["tag_name"]?.AsString() == requestedVersionString &&
                    s?["prerelease"]?.AsBoolean() == prerelease);

            // If not found, try to get latest with proper version parsing
            if (jsonRelease == null)
            {
                jsonRelease = json
                    .Where(s => s?["prerelease"]?.AsBoolean() == prerelease)
                    .Select(s =>
                    {
                        var tagName = s?["tag_name"]?.AsString();
                        if (tagName != null && tagName.Length > 1 && tagName.StartsWith('v') &&
                            Version.TryParse(tagName[1..], out var version))
                        {
                            return new { JsonObject = s, Version = version };
                        }
                        return null;
                    })
                    .OfType<dynamic>()
                    .OrderByDescending(s => s.Version)
                    .Select(s => s.JsonObject)
                    .FirstOrDefault();
            }

            // Assert - Should be null since no valid versions exist
            Assert.IsNull(jsonRelease);
        }

        [TestMethod]
        public void TestVersionParsingLogic_MixedValidInvalidTags()
        {
            // Arrange
            var requestedVersion = new Version("1.0.0");
            var requestedVersionString = $"v{requestedVersion.ToString(3)}";
            var prerelease = false;

            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""invalid-tag"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": []
                }},
                {{
                    ""tag_name"": ""v2.1.0"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": [
                        {{
                            ""name"": ""TestPlugin.zip"",
                            ""browser_download_url"": ""https://example.com/TestPlugin-v2.1.zip""
                        }}
                    ]
                }},
                {{
                    ""tag_name"": ""v"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": []
                }},
                {{
                    ""tag_name"": ""v1.5.0"",
                    ""prerelease"": {prerelease.ToString().ToLower()},
                    ""assets"": [
                        {{
                            ""name"": ""TestPlugin.zip"",
                            ""browser_download_url"": ""https://example.com/TestPlugin-v1.5.zip""
                        }}
                    ]
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);

            // Act - First try exact match
            var jsonRelease = json
                .FirstOrDefault(s =>
                    s?["tag_name"]?.AsString() == requestedVersionString &&
                    s?["prerelease"]?.AsBoolean() == prerelease);

            // If not found, get latest valid version
            if (jsonRelease == null)
            {
                jsonRelease = json
                    .Where(s => s?["prerelease"]?.AsBoolean() == prerelease)
                    .Select(s =>
                    {
                        var tagName = s?["tag_name"]?.AsString();
                        if (tagName != null && tagName.Length > 1 && tagName.StartsWith('v') &&
                            Version.TryParse(tagName[1..], out var version))
                        {
                            return new { JsonObject = s, Version = version };
                        }
                        return null;
                    })
                    .OfType<dynamic>()
                    .OrderByDescending(s => s.Version)
                    .Select(s => s.JsonObject)
                    .FirstOrDefault();
            }

            // Assert - Should get v2.1.0 as it's the latest valid version
            Assert.IsNotNull(jsonRelease);
            Assert.AreEqual("v2.1.0", jsonRelease["tag_name"]?.AsString());
        }

        [TestMethod]
        public void TestVersionParsingLogic_PrereleaseFiltering()
        {
            // Arrange
            var requestedVersion = new Version("1.0.0");
            var requestedVersionString = $"v{requestedVersion.ToString(3)}";
            var prerelease = false; // We want stable releases only

            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""v2.0.0"",
                    ""prerelease"": true,
                    ""assets"": [
                        {{
                            ""name"": ""TestPlugin.zip"",
                            ""browser_download_url"": ""https://example.com/TestPlugin-v2-pre.zip""
                        }}
                    ]
                }},
                {{
                    ""tag_name"": ""v1.5.0"",
                    ""prerelease"": false,
                    ""assets"": [
                        {{
                            ""name"": ""TestPlugin.zip"",
                            ""browser_download_url"": ""https://example.com/TestPlugin-v1.5.zip""
                        }}
                    ]
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);

            // Act - First try exact match
            var jsonRelease = json
                .FirstOrDefault(s =>
                    s?["tag_name"]?.AsString() == requestedVersionString &&
                    s?["prerelease"]?.AsBoolean() == prerelease);

            // If not found, get latest stable version (not prerelease)
            if (jsonRelease == null)
            {
                jsonRelease = json
                    .Where(s => s?["prerelease"]?.AsBoolean() == prerelease)
                    .Select(s =>
                    {
                        var tagName = s?["tag_name"]?.AsString();
                        if (tagName != null && tagName.Length > 1 && tagName.StartsWith('v') &&
                            Version.TryParse(tagName[1..], out var version))
                        {
                            return new { JsonObject = s, Version = version };
                        }
                        return null;
                    })
                    .OfType<dynamic>()
                    .OrderByDescending(s => s.Version)
                    .Select(s => s.JsonObject)
                    .FirstOrDefault();
            }

            // Assert - Should get v1.5.0 (stable) not v2.0.0 (prerelease)
            Assert.IsNotNull(jsonRelease);
            Assert.AreEqual("v1.5.0", jsonRelease["tag_name"]?.AsString());
            Assert.AreEqual(false, jsonRelease["prerelease"]?.AsBoolean());
        }

        [TestMethod]
        public void TestAssetSelection_CorrectPluginName()
        {
            // Arrange
            var pluginName = "RpcServer";
            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""v1.0.0"",
                    ""prerelease"": false,
                    ""assets"": [
                        {{
                            ""name"": ""ApplicationLogs.zip"",
                            ""browser_download_url"": ""https://example.com/ApplicationLogs.zip""
                        }},
                        {{
                            ""name"": ""RpcServer.zip"",
                            ""browser_download_url"": ""https://example.com/RpcServer.zip""
                        }},
                        {{
                            ""name"": ""OracleService.zip"",
                            ""browser_download_url"": ""https://example.com/OracleService.zip""
                        }}
                    ]
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);
            var jsonRelease = json.FirstOrDefault();

            // Act
            var jsonAssets = (JArray)jsonRelease["assets"];
            var jsonPlugin = jsonAssets
                .FirstOrDefault(s =>
                    Path.GetFileNameWithoutExtension(s?["name"]?.AsString() ?? string.Empty)
                        .Equals(pluginName, StringComparison.InvariantCultureIgnoreCase));

            // Assert
            Assert.IsNotNull(jsonPlugin);
            Assert.AreEqual("RpcServer.zip", jsonPlugin["name"]?.AsString());
            Assert.AreEqual("https://example.com/RpcServer.zip", jsonPlugin["browser_download_url"]?.AsString());
        }

        [TestMethod]
        public void TestAssetSelection_PluginNotFound()
        {
            // Arrange
            var pluginName = "NonExistentPlugin";
            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""v1.0.0"",
                    ""prerelease"": false,
                    ""assets"": [
                        {{
                            ""name"": ""ApplicationLogs.zip"",
                            ""browser_download_url"": ""https://example.com/ApplicationLogs.zip""
                        }},
                        {{
                            ""name"": ""RpcServer.zip"",
                            ""browser_download_url"": ""https://example.com/RpcServer.zip""
                        }}
                    ]
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);
            var jsonRelease = json.FirstOrDefault();

            // Act
            var jsonAssets = (JArray)jsonRelease["assets"];
            var jsonPlugin = jsonAssets
                .FirstOrDefault(s =>
                    Path.GetFileNameWithoutExtension(s?["name"]?.AsString() ?? string.Empty)
                        .Equals(pluginName, StringComparison.InvariantCultureIgnoreCase));

            // Assert
            Assert.IsNull(jsonPlugin);
        }

        [TestMethod]
        public void TestVersionComparison_OrderingLogic()
        {
            // Arrange
            var jsonResponse = $@"[
                {{
                    ""tag_name"": ""v1.0.0"",
                    ""prerelease"": false,
                    ""assets"": []
                }},
                {{
                    ""tag_name"": ""v2.1.0"",
                    ""prerelease"": false,
                    ""assets"": []
                }},
                {{
                    ""tag_name"": ""v2.0.0"",
                    ""prerelease"": false,
                    ""assets"": []
                }},
                {{
                    ""tag_name"": ""v1.5.0"",
                    ""prerelease"": false,
                    ""assets"": []
                }}
            ]";

            var json = (JArray)JToken.Parse(jsonResponse);

            // Act - Get versions in descending order
            var versions = json
                .Where(s => s?["prerelease"]?.AsBoolean() == false)
                .Select(s =>
                {
                    var tagName = s?["tag_name"]?.AsString();
                    if (tagName != null && tagName.Length > 1 && tagName.StartsWith('v') &&
                        Version.TryParse(tagName[1..], out var version))
                    {
                        return new { JsonObject = s, Version = version };
                    }
                    return null;
                })
                .OfType<dynamic>()
                .OrderByDescending(s => s.Version)
                .Select(s => s.JsonObject["tag_name"]?.AsString())
                .ToList();

            // Assert - Should be ordered: v2.1.0, v2.0.0, v1.5.0, v1.0.0
            Assert.AreEqual(4, versions.Count);
            Assert.AreEqual("v2.1.0", versions[0]);
            Assert.AreEqual("v2.0.0", versions[1]);
            Assert.AreEqual("v1.5.0", versions[2]);
            Assert.AreEqual("v1.0.0", versions[3]);
        }
    }
}
