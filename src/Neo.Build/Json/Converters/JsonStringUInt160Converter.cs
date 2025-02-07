// Copyright (C) 2015-2025 The Neo Project.
//
// JsonUInt160Converter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions.Json;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Json.Converters
{
    internal class JsonStringUInt160Converter : JsonConverter<UInt160>
    {
        public override UInt160? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();

            if (UInt160.TryParse(value, out var scriptHash) == false)
                throw new JsonInvalidFormatException(value);

            return scriptHash;
        }

        public override void Write(Utf8JsonWriter writer, UInt160 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value}");
        }
    }
}
