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

using Neo.Build.Exceptions.Json;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Json.Converters
{
    internal class JsonStringECPointConverter : JsonConverter<ECPoint>
    {
        public override ECPoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var valueString = reader.GetString();

            if (reader.TryGetBytesFromBase64(out var value) == false)
                throw new JsonInvalidFormatException(valueString);

            return ECPoint.FromBytes(value, ECCurve.Secp256r1);
        }

        public override void Write(Utf8JsonWriter writer, ECPoint value, JsonSerializerOptions options)
        {
            writer.WriteBase64StringValue(value.ToArray());
        }
    }
}
