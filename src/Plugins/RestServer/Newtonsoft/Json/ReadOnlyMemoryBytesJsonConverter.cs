// Copyright (C) 2015-2024 The Neo Project.
//
// ReadOnlyMemoryBytesJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    public class ReadOnlyMemoryBytesJsonConverter : JsonConverter<ReadOnlyMemory<byte>>
    {
        public override ReadOnlyMemory<byte> ReadJson(JsonReader reader, Type objectType, ReadOnlyMemory<byte> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var o = JToken.Load(reader);
            var value = o.ToObject<string>();
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Convert.FromBase64String(value);
        }

        public override void WriteJson(JsonWriter writer, ReadOnlyMemory<byte> value, JsonSerializer serializer)
        {
            writer.WriteValue(Convert.ToBase64String(value.Span));
        }
    }
}
