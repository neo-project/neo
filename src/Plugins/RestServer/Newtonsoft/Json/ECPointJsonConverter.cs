// Copyright (C) 2015-2024 The Neo Project.
//
// ECPointJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Plugins.RestServer.Exceptions;
using Newtonsoft.Json;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class ECPointJsonConverter : JsonConverter<ECPoint>
    {
        public override ECPoint ReadJson(JsonReader reader, Type objectType, ECPoint? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader?.Value?.ToString();
            try
            {
                return ECPoint.Parse(value, ECCurve.Secp256r1);
            }
            catch (FormatException)
            {
                throw new UInt256FormatException($"'{value}' is invalid.");
            }
        }

        public override void WriteJson(JsonWriter writer, ECPoint? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            writer.WriteValue(value.ToString());
        }
    }
}
