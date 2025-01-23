// Copyright (C) 2015-2025 The Neo Project.
//
// GuidJsonConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Newtonsoft.Json;
using System;

namespace Neo.Plugins.RestServer.Newtonsoft.Json
{
    internal class GuidJsonConverter : JsonConverter<Guid>
    {
        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString();
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Guid.Parse(value);
        }

        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString("n"));
        }
    }
}
