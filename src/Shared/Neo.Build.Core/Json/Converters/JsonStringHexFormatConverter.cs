// Copyright (C) 2015-2025 The Neo Project.
//
// JsonStringHexFormatConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Extensions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Json.Converters
{
    public class JsonStringHexFormatConverter : JsonConverter<byte[]?>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new NeoBuildInvalidHexFormatException();

            var valueString = reader.GetString();

            if (string.IsNullOrEmpty(valueString))
                return default;

            return valueString.HexToBytes();
        }

        public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToHexString());
        }
    }
}
