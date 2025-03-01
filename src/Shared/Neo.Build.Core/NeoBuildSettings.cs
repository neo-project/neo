// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Interfaces;
using Neo.Build.Core.Json;
using Neo.Build.Core.Models;
using Neo.Build.Core.Models.SmartContract;
using Neo.Build.Core.SmartContract;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo.Build.Core
{
    public class NeoBuildSettings
    {
        public NeoBuildSettings(JsonNode jsonExtras, JsonSerializerOptions? options = null)
        {
            _jsonExtras = jsonExtras;
            _options = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            ProtocolSettings = GetObject<ProtocolSettingsModel, ProtocolSettings>(JsonPropertyNames.ProtocolSettings, NeoBuildDefaults.ProtocolSettings);
            ApplicationEngineSettings = GetObject<ApplicationEngineSettingsModel, ApplicationEngineSettings>(JsonPropertyNames.ProtocolSettings, ApplicationEngineDefaults.Settings);
        }

        public ProtocolSettings ProtocolSettings { get; }
        public ApplicationEngineSettings ApplicationEngineSettings { get; }

        private readonly JsonSerializerOptions _options;
        private readonly JsonNode _jsonExtras;

        [return: NotNullIfNotNull(nameof(defaultValue))]
        private TResult? GetObject<TModel, TResult>(string? propertyName, TResult? defaultValue)
            where TResult : notnull
            where TModel : notnull, JsonModel
        {
            var jsonNode = string.IsNullOrEmpty(propertyName) ? _jsonExtras : _jsonExtras[propertyName];
            if (jsonNode is null)
                return defaultValue;

            var model = JsonModel.FromJson<TModel>(jsonNode.ToJsonString(), _options);
            if (model is not IConvertToObject<TResult> result)
                return defaultValue;

            return result.ToObject();
        }
    }
}
