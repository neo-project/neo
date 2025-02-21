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

using Neo.Build.Core.Exceptions;
using Neo.Build.Core.Json;
using Neo.Build.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo.Build.Core
{
    public class NeoBuildSettings
    {
        public NeoBuildSettings(JsonNode jsonExtras, JsonSerializerOptions? options = null)
        {
            _options = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            ProtocolSettings = BindJsonNodeToProtocolSettings(jsonExtras[JsonPropertyNames.ProtocolSettings]);
        }

        public ProtocolSettings ProtocolSettings { get; }

        private readonly JsonSerializerOptions _options;

        private ProtocolSettings BindJsonNodeToProtocolSettings(JsonNode? protocolSettingNode)
        {
            if (protocolSettingNode is null)
                return NeoBuildDefaults.ProtocolDefaultSettings;

            var protocolSettingModel = JsonModel.FromJson<ProtocolSettingsModel>(protocolSettingNode.ToJsonString(), _options);

            // TODO: Create new "NeoBuildException" class for this error and error code.
            if (protocolSettingModel is null)
                throw new NeoBuildException(string.Empty); // This function shouldn't reach here

            return protocolSettingModel.ToObject();
        }
    }
}
