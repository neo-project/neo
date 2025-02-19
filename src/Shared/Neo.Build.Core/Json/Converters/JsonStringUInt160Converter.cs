// Copyright (C) 2015-2025 The Neo Project.
//
// JsonStringUInt160Converter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Json.Converters
{
    public class JsonStringUInt160Converter : JsonConverter<UInt160>
    {
        public override UInt160? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new NeoBuildInvalidScriptHashFormatException();

            var valueString = reader.GetString();

            if (UInt160.TryParse(valueString, out var scriptHash) == false)
                throw new NeoBuildInvalidScriptHashFormatException();

            return scriptHash;
        }

        public override void Write(Utf8JsonWriter writer, UInt160 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
