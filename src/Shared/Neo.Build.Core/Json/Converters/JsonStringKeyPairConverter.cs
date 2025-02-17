// Copyright (C) 2015-2025 The Neo Project.
//
// JsonStringKeyPairConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Extensions;
using Neo.Wallets;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Json.Converters
{
    public class JsonStringKeyPairConverter : JsonConverter<KeyPair>
    {
        public override KeyPair? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var valueString = reader.GetString();

            if (string.IsNullOrEmpty(valueString))
                throw new NeoBuildInvalidHexFormatException();

            var valueBytes = valueString.HexToBytes();

            return new(valueBytes);
        }

        public override void Write(Utf8JsonWriter writer, KeyPair value, JsonSerializerOptions options)
        {
            var valueBytes = value.PrivateKey;
            writer.WriteStringValue(valueBytes.ToHexString());
        }
    }
}
