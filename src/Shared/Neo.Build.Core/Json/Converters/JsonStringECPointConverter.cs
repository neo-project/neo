// Copyright (C) 2015-2025 The Neo Project.
//
// JsonStringECPointConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Cryptography.ECC;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Json.Converters
{
    public class JsonStringECPointConverter : JsonConverter<ECPoint?>
    {
        public override ECPoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new NeoBuildInvalidECPointFormatException();

            var valueString = reader.GetString();

            if (string.IsNullOrEmpty(valueString))
                return default;

            if (ECPoint.TryParse(valueString, ECCurve.Secp256r1, out var value) == false)
                throw new NeoBuildInvalidECPointFormatException();

            return value;
        }

        public override void Write(Utf8JsonWriter writer, ECPoint? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value?.ToString());
        }
    }
}
