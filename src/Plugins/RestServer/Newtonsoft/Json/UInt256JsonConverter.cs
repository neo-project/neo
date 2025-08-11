// Copyright (C) 2015-2025 The Neo Project.
//
// UInt256JsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.RestServer.Exceptions;
using Newtonsoft.Json;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class UInt256JsonConverter : JsonConverter<UInt256>
    {
        public override UInt256 ReadJson(JsonReader reader, Type objectType, UInt256? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString();
            if (value is null) throw new ArgumentNullException(nameof(value));

            try
            {
                return UInt256.Parse(value);
            }
            catch (FormatException)
            {
                throw new UInt256FormatException($"'{value}' is invalid.");
            }
        }

        public override void WriteJson(JsonWriter writer, UInt256? value, JsonSerializer serializer)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            writer.WriteValue(value.ToString());
        }
    }
}
