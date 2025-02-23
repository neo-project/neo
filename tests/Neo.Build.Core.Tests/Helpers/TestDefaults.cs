// Copyright (C) 2015-2025 The Neo Project.
//
// TestDefaults.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Json.Converters;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Tests.Helpers
{
    internal static class TestDefaults
    {
        public static readonly JsonSerializerOptions JsonDefaultSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            WriteIndented = false,
            RespectNullableAnnotations = false,
            Converters =
            {
                // TODO: Make sure you add the same converters from NeoBuildDefaults.JsonDefaultSerializerOptions.Converters
                // NOTE: JsonConverterAttribute overrides these converters
                new JsonStringEnumConverter(),
                new JsonStringECPointConverter(),
                new JsonStringUInt160Converter(),
            }
        };
    }
}
